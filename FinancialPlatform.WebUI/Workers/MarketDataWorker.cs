using System;
using System.Collections.Generic;
using FinancialPlatform.Application.DTOs;
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
        private readonly List<MarketTick> _dbBuffer = new(); // Bộ nhớ đệm gom lô trước khi lưu DB

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
                        var lastQuote = _historyBuffer.LastOrDefault();
                        var quoteDate = tick.Timestamp;
                        
                        // Thư viện Skender yêu cầu Date phải luôn tăng dần và không được trùng lặp
                        if (lastQuote != null && quoteDate <= lastQuote.Date)
                        {
                            quoteDate = lastQuote.Date.AddMilliseconds(1);
                        }

                        // Skender.Stock.Indicators dùng class Quote
                        _historyBuffer.Add(new Quote
                        {
                            Date = quoteDate,
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
                        try
                        {
                            // Skender an toàn nhất khi có từ 15 nến trở lên
                            if (_historyBuffer.Count >= 15)
                            {
                                var rsiResults = _historyBuffer.GetRsi(14);
                                var latestRsi = rsiResults.LastOrDefault();
                                
                                if (latestRsi != null && latestRsi.Rsi != null)
                                {
                                    tick.RSI = (double)latestRsi.Rsi;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"Chưa đủ dữ liệu để tính RSI hoặc lỗi RSI: {ex.Message}");
                        }

                        // 4. Log ra màn hình console để kiểm tra
                        _logger.LogInformation($"Symbol: {tick.Symbol} | Price: {tick.Price:C} | RSI: {tick.RSI:F2}");

                        // 5. Tạo DTO và bắn data qua SignalR tới đúng group client đang nghe mã này
                        var marketDataDto = new MarketDataDto
                        {
                            Symbol = tick.Symbol,
                            Price = tick.Price,
                            RSI = tick.RSI,
                            Timestamp = tick.Timestamp
                        };
                        
                        // Gửi tới group có tên trùng với mã symbol (ví dụ: "BTCUSDT")
                        await _hubContext.Clients.Group(tick.Symbol).SendAsync("ReceiveMarketUpdate", marketDataDto, stoppingToken);

                        // 6. Tối ưu Giai đoạn 2: Gom lô (Batching) chống nghẽn SQL Server
                        _dbBuffer.Add(tick);
                        if (_dbBuffer.Count >= 15) // Gom 15 lần lấy giá (khoảng 30 giây) mới lưu 1 lần
                        {
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var repository = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();
                                foreach (var item in _dbBuffer)
                                {
                                    await repository.AddTickAsync(item);
                                }
                            }
                            _logger.LogInformation($"[DB] Đã lưu {_dbBuffer.Count} records vào SQL Server.");
                            _dbBuffer.Clear();
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
