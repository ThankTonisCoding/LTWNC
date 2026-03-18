using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FinancialPlatform.Application.DTOs;
using FinancialPlatform.Core.Interfaces;
using FinancialPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinancialPlatform.WebUI.Workers
{
    public class RiskManagementWorker : BackgroundService
    {
        private readonly ILogger<RiskManagementWorker> _logger;
        private readonly IServiceProvider _serviceProvider;

        public RiskManagementWorker(ILogger<RiskManagementWorker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Risk Management Worker started.");

            // Đợi 10s để hệ thống nạp xong Redis/SQL trước khi quét
            await Task.Delay(10000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var tradingService = scope.ServiceProvider.GetRequiredService<ITradingService>();
                        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

                        // Lấy tất cả các lệnh đang Mở (Filled)
                        var openOrders = await context.Orders
                            .Include(o => o.Asset)
                            .Where(o => o.Status == "Filled" && (o.StopLoss != null || o.TakeProfit != null))
                            .ToListAsync(stoppingToken);

                        foreach (var order in openOrders)
                        {
                            var cacheKey = $"LATEST_PRICE_{order.Asset?.Symbol}";
                            var cachedData = await cache.GetStringAsync(cacheKey, stoppingToken);
                            if (string.IsNullOrEmpty(cachedData)) continue;

                            var tick = JsonSerializer.Deserialize<MarketDataDto>(cachedData);
                            if (tick == null) continue;

                            decimal currentPrice = tick.Price;
                            bool shouldClose = false;

                            if (order.Side == "Buy")
                            {
                                if (order.StopLoss.HasValue && currentPrice <= order.StopLoss.Value)
                                    shouldClose = true;
                                if (order.TakeProfit.HasValue && currentPrice >= order.TakeProfit.Value)
                                    shouldClose = true;
                            }
                            else if (order.Side == "Sell")
                            {
                                if (order.StopLoss.HasValue && currentPrice >= order.StopLoss.Value)
                                    shouldClose = true;
                                if (order.TakeProfit.HasValue && currentPrice <= order.TakeProfit.Value)
                                    shouldClose = true;
                            }

                            if (shouldClose)
                            {
                                _logger.LogInformation($"[RiskManagement] Đang đóng lệnh {order.Id} do chạm giới hạn (Giá hiện tại: {currentPrice})");
                                await tradingService.ClosePositionAsync(order.Id, currentPrice);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi trong RiskManagementWorker");
                }

                // Quét mỗi giây 1 lần
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
