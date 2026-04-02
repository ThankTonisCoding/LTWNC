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
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using Skender.Stock.Indicators;

namespace FinancialPlatform.WebUI.Workers
{
    public class MarketDataWorker : BackgroundService
    {
        private readonly ILogger<MarketDataWorker> _logger;
        private readonly BinanceService _binanceService;
        private readonly IHubContext<MarketHub> _hubContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDistributedCache _cache;
        
        // Mở rộng thành 6 tài sản Crypto hàng đầu
        private readonly string[] _symbols = { "BTCUSDT", "ETHUSDT", "BNBUSDT", "SOLUSDT", "ADAUSDT", "XRPUSDT" };
        private readonly Dictionary<string, List<Quote>> _historyBuffers = new(); 
        private readonly Dictionary<string, List<MarketTick>> _dbBuffers = new(); 
        private readonly Dictionary<string, DateTime> _lastAIPredictionTime = new();
        private readonly Dictionary<string, string> _aiSignals = new();

        public MarketDataWorker(ILogger<MarketDataWorker> logger, BinanceService binanceService, IHubContext<MarketHub> hubContext, IServiceProvider serviceProvider, IDistributedCache cache)
        {
            _logger = logger;
            _binanceService = binanceService;
            _hubContext = hubContext;
            _serviceProvider = serviceProvider;
            _cache = cache;

            // Khởi tạo bộ đệm riêng biệt cho từng Symbol
            foreach (var symbol in _symbols)
            {
                _historyBuffers[symbol] = new List<Quote>();
                _dbBuffers[symbol] = new List<MarketTick>();
                _lastAIPredictionTime[symbol] = DateTime.UtcNow.AddSeconds(-20); // Đảm bảo lần đầu chạy ngay lập tức
                _aiSignals[symbol] = "HOLD";
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Market Data Worker started for Multiple Assets.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Chạy lấy giá song song định kỳ cho tất cả 6 Symbols
                    var tasks = _symbols.Select(symbol => ProcessSymbolAsync(symbol, stoppingToken));
                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in MarketDataWorker loop: {ex.Message}");
                }

                await Task.Delay(2000, stoppingToken);
            }
        }

        private async Task ProcessSymbolAsync(string symbol, CancellationToken stoppingToken)
        {
            try
            {
                var tick = await _binanceService.GetTickerAsync(symbol);
                if (tick == null) return;

                var history = _historyBuffers[symbol];
                var dbBuffer = _dbBuffers[symbol];

                var lastQuote = history.LastOrDefault();
                var quoteDate = tick.Timestamp;
                
                if (lastQuote != null && quoteDate <= lastQuote.Date)
                {
                    quoteDate = lastQuote.Date.AddMilliseconds(1);
                }

                history.Add(new Quote
                {
                    Date = quoteDate,
                    Close = tick.Price,
                    Open = tick.Price,
                    High = tick.Price,
                    Low = tick.Price,
                    Volume = 0
                });

                if (history.Count > 200) history.RemoveAt(0);

                IEnumerable<RsiResult>? rsiResults = null;
                try
                {
                    if (history.Count >= 15)
                    {
                        rsiResults = history.GetRsi(14);
                        var latestRsi = rsiResults.LastOrDefault();
                        if (latestRsi != null && latestRsi.Rsi.HasValue) tick.RSI = latestRsi.Rsi.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Lỗi phân tích RSI của {symbol}: {ex.Message}");
                }

                string aiSignal = _aiSignals[symbol];

                // Giải quyết triệt để lỗi thắt cổ chai CPU (CPU Bottleneck):
                // Chỉ chạy model AI (ML.NET) 10 giây 1 lần cho mỗi symbol, thay vì mỗi 2 giây
                if ((DateTime.UtcNow - _lastAIPredictionTime[symbol]).TotalSeconds > 10)
                {
                    try 
                    {
                        if (rsiResults != null && rsiResults.Any())
                        {
                            var rsiHistory = rsiResults.Where(x => x.Rsi.HasValue).Select(x => x.Rsi.Value).TakeLast(15);
                            using var scope = _serviceProvider.CreateScope();
                            var predictor = scope.ServiceProvider.GetRequiredService<IMarketPredictorService>();
                            aiSignal = predictor.PredictSignal(rsiHistory);

                            // Cập nhật Cache
                            _aiSignals[symbol] = aiSignal;
                            _lastAIPredictionTime[symbol] = DateTime.UtcNow;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Lỗi AI cho {symbol}: {ex.Message}");
                    }
                }

                if (tick.RSI.HasValue && (double.IsNaN(tick.RSI.Value) || double.IsInfinity(tick.RSI.Value)))
                {
                    tick.RSI = 0;
                }

                var marketDataDto = new MarketDataDto
                {
                    Symbol = tick.Symbol,
                    Price = tick.Price,
                    RSI = tick.RSI,
                    Timestamp = tick.Timestamp,
                    AISignal = aiSignal
                };
                
                // _logger.LogInformation($"Symbol: {tick.Symbol} | Price: {tick.Price:C}");

                // 1. Lưu giá vào Redis để Caching API
                try 
                {
                    var cacheKey = $"LATEST_PRICE_{symbol}";
                    var cacheOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) };
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(marketDataDto), cacheOptions, stoppingToken);
                } catch { /* Ignore Redis error */ }
                
                // 2. Broadcast giá thời gian thực cho User đang theo dõi Symbol này qua SignalR
                try
                {
                    await _hubContext.Clients.Group(symbol).SendAsync("ReceiveMarketUpdate", marketDataDto, stoppingToken);
                } catch { /* Ignore SignalR error */ }

                // 3. Gom lô (Batching) lưu vào SQL Server Database
                dbBuffer.Add(tick);
                if (dbBuffer.Count >= 15)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var repository = scope.ServiceProvider.GetRequiredService<IMarketDataRepository>();
                        foreach (var item in dbBuffer) { await repository.AddTickAsync(item); }
                        dbBuffer.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"Lỗi SQL Server ({symbol}): {ex.Message}");
                        dbBuffer.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi bất ngờ ở symbol {symbol}: {ex.Message}");
            }
        }
    }
}
