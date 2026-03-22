using System.Linq;
using System.Threading.Tasks;
using FinancialPlatform.Core.Interfaces;
using FinancialPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlatform.Infrastructure.Services
{
    public class AdminQueryService : IAdminQueryService
    {
        private readonly ApplicationDbContext _context;

        public AdminQueryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetDashboardMetricsAsync()
        {
            var totalUsers = await _context.Users.Where(u => u.EmailConfirmed).CountAsync(); // Lọc User Active
            var totalCash = await _context.Portfolios.SumAsync(p => p.CashBalance);

            var totalVolume = await _context.Orders
                .Where(o => o.Status == "Filled" || o.Status == "Closed")
                .SumAsync(o => o.Amount * o.Price);

            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var filledOrders = await _context.Orders.CountAsync(o => o.Status == "Filled");
            var closedOrders = await _context.Orders.CountAsync(o => o.Status == "Closed");

            var recentTrades = await _context.Transactions
                .Include(t => t.User)
                .OrderByDescending(t => t.Timestamp)
                .Take(20)
                .Select(t => new {
                    UserName = t.User != null ? (t.User.Email ?? t.User.UserName) : "Unknown",
                    Type = t.Type,
                    Amount = t.Amount,
                    Description = t.Description,
                    Timestamp = t.Timestamp
                })
                .ToListAsync();

            return new
            {
                TotalUsers = totalUsers,
                TotalCash = totalCash,
                TotalVolume = totalVolume,
                OrderStats = new { Pending = pendingOrders, Filled = filledOrders, Closed = closedOrders },
                RecentTrades = recentTrades
            };
        }
    }
}
