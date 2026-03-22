using System.Linq;
using System.Threading.Tasks;
using FinancialPlatform.Application.DTOs;
using FinancialPlatform.Core.Entities;
using FinancialPlatform.Infrastructure.Data;
using FinancialPlatform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinancialPlatform.Tests
{
    public class TradingServiceTests
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task InitializePaperWalletAsync_Should_Create_User_And_Portfolio()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<TradingService>>();
            var service = new TradingService(context, mockCache.Object, mockLogger.Object);

            string testUserId = "user-123";

            // Act
            await service.InitializePaperWalletAsync(testUserId);

            // Assert
            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == testUserId);
            var portfolio = await context.Portfolios.FirstOrDefaultAsync(p => p.UserId == testUserId);

            Assert.NotNull(user);
            Assert.NotNull(portfolio);
            Assert.Equal(10000m, portfolio.CashBalance);
        }

        [Fact]
        public async Task PlaceOrderAsync_Buy_Market_Should_Fail_If_Insufficient_Funds()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<TradingService>>();
            var service = new TradingService(context, mockCache.Object, mockLogger.Object);

            string userId = "user-no-money";
            await service.InitializePaperWalletAsync(userId);
            
            // Set balance to 0
            var portfolio = await context.Portfolios.FirstAsync(p => p.UserId == userId);
            portfolio.CashBalance = 0;
            await context.SaveChangesAsync();

            // Mock price (Add asset and tick)
            var asset = new Asset { Symbol = "BTCUSDT", Name = "Bitcoin" };
            context.Assets.Add(asset);
            context.MarketTicks.Add(new MarketTick { Symbol = "BTCUSDT", Price = 50000m, Timestamp = System.DateTime.UtcNow });
            await context.SaveChangesAsync();

            // Act
            var order = await service.PlaceOrderAsync(userId, "BTCUSDT", "Buy", "Market", 1m);

            // Assert
            Assert.NotNull(order);
            Assert.Equal("Cancelled", order.Status); // Should cancel because 0 < 50000
        }
        
        [Fact]
        public async Task PlaceOrderAsync_Buy_Market_Should_Succeed_If_Sufficient_Funds()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var mockCache = new Mock<IDistributedCache>();
            var mockLogger = new Mock<ILogger<TradingService>>();
            var service = new TradingService(context, mockCache.Object, mockLogger.Object);

            string userId = "user-rich";
            await service.InitializePaperWalletAsync(userId); // 10,000 balance

            var asset = new Asset { Symbol = "ETHUSDT", Name = "Ethereum" };
            context.Assets.Add(asset);
            context.MarketTicks.Add(new MarketTick { Symbol = "ETHUSDT", Price = 3000m, Timestamp = System.DateTime.UtcNow });
            await context.SaveChangesAsync();

            // Act: Buy 2 ETH = $6,000
            var order = await service.PlaceOrderAsync(userId, "ETHUSDT", "Buy", "Market", 2m);

            // Assert
            Assert.NotNull(order);
            Assert.Equal("Filled", order.Status);
            
            var portfolio = await context.Portfolios.FirstAsync(p => p.UserId == userId);
            Assert.Equal(4000m, portfolio.CashBalance); // 10000 - 6000
            
            var transaction = await context.Transactions.FirstOrDefaultAsync(t => t.UserId == userId);
            Assert.NotNull(transaction);
            Assert.Equal("Trade", transaction.Type);
            Assert.Equal(-6000m, transaction.Amount);
        }
    }
}
