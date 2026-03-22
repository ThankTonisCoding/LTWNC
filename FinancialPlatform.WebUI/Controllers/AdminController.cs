using System.Linq;
using System.Threading.Tasks;
using FinancialPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinancialPlatform.WebUI.Controllers
{
    [Authorize(Roles = "Admin")] // Bảo mật API: Chỉ Admin
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalCash = await _context.Portfolios.SumAsync(p => p.CashBalance);
            
            // Tính toán Trading Volume (tổng USD đã khớp lệnh)
            var totalVolume = await _context.Orders
                .Where(o => o.Status == "Filled" || o.Status == "Closed")
                .SumAsync(o => o.Amount * o.Price);

            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var filledOrders = await _context.Orders.CountAsync(o => o.Status == "Filled");
            var closedOrders = await _context.Orders.CountAsync(o => o.Status == "Closed");

            // Lấy 20 lịch sử giao dịch gần nhất
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

            return Ok(new
            {
                TotalUsers = totalUsers,
                TotalCash = totalCash,
                TotalVolume = totalVolume,
                OrderStats = new { Pending = pendingOrders, Filled = filledOrders, Closed = closedOrders },
                RecentTrades = recentTrades
            });
        }
    }
}
