using System.Linq;
using System.Threading.Tasks;
using FinancialPlatform.Core.Interfaces;
using FinancialPlatform.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlatform.Infrastructure.Services
{
    public class PortfolioQueryService : IPortfolioQueryService
    {
        private readonly ApplicationDbContext _context;

        public PortfolioQueryService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<object?> GetPortfolioAsync(string userId)
        {
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.UserId == userId);
            if (portfolio == null) return null;

            var openOrders = await _context.Orders
                .Include(o => o.Asset)
                // "Filled" nghĩa là lệnh Market đã khớp và đang giữ vị thế (Position)
                .Where(o => o.UserId == userId && o.Status == "Filled") 
                .Select(o => new 
                {
                    o.Id,
                    Symbol = o.Asset != null ? o.Asset.Symbol : "UNKNOWN",
                    o.Side,
                    o.Amount,
                    EntryPrice = o.Price,
                    o.StopLoss,
                    o.TakeProfit,
                    o.CreatedAt
                })
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            var transactionHistory = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Timestamp)
                .Take(10)
                .ToListAsync();

            return new 
            {
                UserId = userId,
                CashBalance = portfolio.CashBalance,
                OpenOrders = openOrders,
                RecentTransactions = transactionHistory
            };
        }
    }
}
