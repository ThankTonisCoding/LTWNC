using System.Linq;
using System.Threading.Tasks;
using FinancialPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlatform.WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PortfolioController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách Lệnh đang mở và Số dư ví
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetPortfolio(string userId)
        {
            var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.UserId == userId);
            if (portfolio == null) 
                return NotFound(new { Message = "User chưa được cấp ví ảo." });

            var openOrders = await _context.Orders
                .Include(o => o.Asset)
                .Where(o => o.UserId == userId && o.Status == "Filled") // Chỉ lấy lệnh đang giữ
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
                .Take(10) // Lấy 10 lịch sử gần nhất
                .ToListAsync();

            return Ok(new 
            {
                UserId = userId,
                CashBalance = portfolio.CashBalance,
                OpenOrders = openOrders,
                RecentTransactions = transactionHistory
            });
        }
    }
}
