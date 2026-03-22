using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FinancialPlatform.Application.DTOs;
using FinancialPlatform.Core.Entities;
using FinancialPlatform.Core.Interfaces;
using FinancialPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace FinancialPlatform.Infrastructure.Services
{
    public class TradingService : ITradingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDistributedCache _cache;
        private readonly ILogger<TradingService> _logger;

        public TradingService(ApplicationDbContext context, IDistributedCache cache, ILogger<TradingService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task InitializePaperWalletAsync(string userId)
        {
            // Tự động tạo User ảo nếu chưa có để tránh lỗi Khóa ngoại (Foreign Key Constraint)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                user = new ApplicationUser { Id = userId, UserName = "testuser", Email = "testuser@finance.local" };
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }

            var exists = await _context.Portfolios.AnyAsync(p => p.UserId == userId);
            if (!exists)
            {
                var portfolio = new Portfolio
                {
                    UserId = userId,
                    CashBalance = 10000m // 10,000 USD tiền ảo
                };
                _context.Portfolios.Add(portfolio);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Khởi tạo Ví ảo 10.000$ thành công cho User {userId}");
            }
        }

        public async Task<Order> PlaceOrderAsync(string userId, string symbol, string side, string type, decimal amount, decimal? limitPrice = null, decimal? stopLoss = null, decimal? takeProfit = null)
        {
            var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Symbol == symbol);
            if (asset == null)
            {
                // Tự động tạo Asset nếu chưa có
                asset = new Asset { Symbol = symbol, Name = symbol };
                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();
            }

            var order = new Order
            {
                UserId = userId,
                AssetId = asset.Id,
                Side = side,
                Type = type,
                Amount = amount,
                Price = limitPrice ?? 0, // Sẽ được cập nhật nếu là Market Order
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
                Status = "Pending"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            if (type == "Market")
            {
                await ExecuteMarketOrderAsync(order);
            }

            return order;
        }

        public async Task<bool> ExecuteMarketOrderAsync(Order order)
        {
            var asset = await _context.Assets.FindAsync(order.AssetId);
            if (asset == null) return false;

            // 1. Lấy giá từ Redis Caching (nếu không có Redis thì fallback xuống SQL)
            string? cachedData = null;
            try 
            {
                var cacheKey = $"LATEST_PRICE_{asset.Symbol}";
                cachedData = await _cache.GetStringAsync(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Redis offline ({ex.Message}). Fallback to SQL DB.");
            }

            decimal currentPrice = 0;
            if (!string.IsNullOrEmpty(cachedData))
            {
                var tick = JsonSerializer.Deserialize<MarketDataDto>(cachedData);
                if (tick != null) currentPrice = tick.Price;
            }
            else
            {
                var latestTick = await _context.MarketTicks.Where(m => m.Symbol == asset.Symbol).OrderByDescending(m => m.Timestamp).FirstOrDefaultAsync();
                if (latestTick != null) currentPrice = latestTick.Price;
            }
            
            if (currentPrice == 0)
            {
                _logger.LogWarning($"Không thể tìm thấy giá của {asset.Symbol} để khớp lệnh!");
                return false;
            }

            // 2. Lấy Portfolio của User
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.UserId == order.UserId);
            if (portfolio == null) return false;

            // 3. Tính toán tiền và Khớp lệnh
            decimal totalCost = order.Amount * currentPrice;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (order.Side == "Buy")
                {
                    if (portfolio.CashBalance < totalCost)
                    {
                        order.Status = "Cancelled";
                        _logger.LogWarning($"User {order.UserId} không đủ tiền mua {order.Amount} {asset.Symbol}. Yêu cầu: {totalCost}, Số dư: {portfolio.CashBalance}");
                    }
                    else
                    {
                        portfolio.CashBalance -= totalCost;
                        order.Price = currentPrice;
                        order.Status = "Filled";

                        // Lưu lịch sử Transaction
                        _context.Transactions.Add(new Transaction
                        {
                            UserId = order.UserId,
                            Type = "Trade",
                            Amount = -totalCost,
                            Description = $"Buy {order.Amount} {asset.Symbol} @ {currentPrice}"
                        });
                    }
                }
                else if (order.Side == "Sell")
                {
                    // Trong hệ thống Margin/Futures, có thể Sell Short. 
                    // Tạm thời đơn giản hóa: Bán khống thoải mái (ví dụ P&L sẽ trừ tiền sau).
                    // Hoặc Bán Spot (phải có coin mới được bán). Giả sử đây là Margin.
                    portfolio.CashBalance += totalCost;
                    order.Price = currentPrice;
                    order.Status = "Filled";

                    _context.Transactions.Add(new Transaction
                    {
                        UserId = order.UserId,
                        Type = "Trade",
                        Amount = totalCost,
                        Description = $"Sell {order.Amount} {asset.Symbol} @ {currentPrice}"
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return order.Status == "Filled";
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning($"Xung đột giao dịch (Race-Condition) khi trừ tiền user {order.UserId}. Mã lệnh: {order.Id}. Lệnh đã bị hủy để bảo vệ toàn vẹn dữ liệu.");
                // Tùy chọn: Có thể cấu hình Retry logic ở đây.
                return false;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Lỗi bất ngờ khi khớp lệnh");
                return false;
            }
        }

        public async Task<bool> ClosePositionAsync(int orderId, decimal? forceClosePrice = null)
        {
            var order = await _context.Orders.Include(o => o.Asset).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null || order.Status != "Filled") return false;

            decimal closePrice = forceClosePrice ?? 0;
            if (!forceClosePrice.HasValue)
            {
                try
                {
                    var cacheKey = $"LATEST_PRICE_{order.Asset.Symbol}";
                    var cachedData = await _cache.GetStringAsync(cacheKey);
                    if (!string.IsNullOrEmpty(cachedData))
                    {
                        var tick = JsonSerializer.Deserialize<MarketDataDto>(cachedData);
                        closePrice = tick?.Price ?? 0;
                    }
                }
                catch {}
                
                if (closePrice == 0)
                {
                    var latestTick = await _context.MarketTicks.Where(m => m.Symbol == order.Asset.Symbol).OrderByDescending(m => m.Timestamp).FirstOrDefaultAsync();
                    if (latestTick != null) closePrice = latestTick.Price;
                }
            }

            if (closePrice == 0) return false;

            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.UserId == order.UserId);

            decimal pnl = 0;
            if (order.Side == "Buy")
            {
                pnl = (closePrice - order.Price) * order.Amount;
            }
            else // Sell Short
            {
                pnl = (order.Price - closePrice) * order.Amount;
            }

            portfolio.CashBalance += pnl; // Cộng lợi nhuận (hoặc trừ lỗ) vào gốc
            order.Status = "Closed";
            order.ClosedAt = DateTime.UtcNow;

            _context.Transactions.Add(new Transaction
            {
                UserId = order.UserId,
                Type = pnl >= 0 ? "Profit" : "Loss",
                Amount = pnl,
                Description = $"Closed {order.Side} Order {order.Id} @ {closePrice}. P&L: {pnl}"
            });

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
