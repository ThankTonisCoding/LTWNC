using System.Threading.Tasks;
using FinancialPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FinancialPlatform.WebUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly ITradingService _tradingService;

        public OrdersController(ITradingService tradingService)
        {
            _tradingService = tradingService;
        }

        [HttpPost("place")]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
        {
            // Hardcode userId "user1" để dễ dàng test (Khi làm Frontend thật, sẽ lấy từ JWT Token/Cookie)
            var userId = "test-user-1"; 

            // Cấp ngay Ví ảo 10.000$ nếu user này chưa từng chơi
            await _tradingService.InitializePaperWalletAsync(userId);

            // Đặt lệnh
            var order = await _tradingService.PlaceOrderAsync(
                userId, 
                request.Symbol, 
                request.Side, 
                request.Type, 
                request.Amount, 
                request.LimitPrice, 
                request.StopLoss, 
                request.TakeProfit
            );

            return Ok(new { Message = "Đã nhận lệnh", OrderDetails = order });
        }
    }

    public class PlaceOrderRequest
    {
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = "Buy"; // "Buy" hoặc "Sell"
        public string Type { get; set; } = "Market"; // "Market" khớp ngay lập tức theo giá Redis
        public decimal Amount { get; set; } // Số lượng coin
        public decimal? LimitPrice { get; set; } // Dành cho Type = "Limit"
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
    }
}
