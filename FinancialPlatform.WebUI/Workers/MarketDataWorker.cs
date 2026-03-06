using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinancialPlatform.Core.Entities;
using FinancialPlatform.Core.Interfaces;
using FinancialPlatform.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using FinancialPlatform.WebUI.Hubs;
using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators; // Thư viện tính chỉ số

namespace FinancialPlatform.WebUI.Workers
{
    public class MarketDataWorker : BackgroundService
    {
        private readonly ILogger<MarketDataWorker> _logger;
        private readonly BinanceService _binanceService;
        private readonly IHubContext<MarketHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        
        // Bộ nhớ tạm để lưu lịch sử giá phục vụ tính RSI (cần ít nhất 14 nến)
        private readonly List<Quote> _historyBuffer = new(); 

        public MarketDataWorker(ILogger<MarketDataWorker> logger, BinanceService binanceService, IHubContext<MarketHub> hubContext, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _binanceService = binanceService;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Market Data Worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 1. Lấy giá BTCUSDT từ Binance
                    var tick = await _binanceService.GetTickerAsync("BTCUSDT");

                    if (tick != null)
                    {
                        // 2. Thêm vào bộ nhớ đệm để tính toán
                        // Skender.Stock.Indicators dùng class Quote
                        _historyBuffer.Add(new Quote
                        {
                            Date = tick.Timestamp,
                            Close = tick.Price,
                            // Các trường khác (Open, High, Low) có thể để tạm bằng Close nếu chỉ tính RSI đơn giản
                            Open = tick.Price,
                            High = tick.Price,
                            Low = tick.Price,
                            Volume = 0
                        });

                        // Giới hạn bộ nhớ đệm (ví dụ chỉ giữ 200 nến gần nhất để tiết kiệm RAM)
                        if (_historyBuffer.Count > 200) 
                        {
                            _historyBuffer.RemoveAt(0);
                        }

                        // 3. Tính chỉ số RSI (Relative Strength Index)
                        // Cần ít nhất 14 nến để tính RSI(14)
                        if (_historyBuffer.Count >= 14)
                        {
                            var rsiResults = _historyBuffer.GetRsi(14);
                            var latestRsi = rsiResults.LastOrDefault();
                            
                            if (latestRsi != null && latestRsi.Rsi != null)
                            {
                                tick.RSI = (double)latestRsi.Rsi;
                            }
                        }

                        // 4. Log ra màn hình console để kiểm tra
                        _logger.LogInformation($"Symbol: {tick.Symbol} | Price: {tick.Price:C} | RSI: {tick.RSI:F2}");

                        // 5. Bắn data qua SignalR tới tất cả client đang nghe
                        await _hubContext.Clients.All.SendAsync("ReceiveMarketUpdate", tick, stoppingToken);

                        // 6. Lưu vào Database
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var repository = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();
                            await repository.AddTickAsync(tick);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching market data");
                }

                // Chờ 2 giây trước khi gọi lần tiếp theo (tránh bị Binance chặn IP)
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
