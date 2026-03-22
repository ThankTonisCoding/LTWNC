using System.Threading.Tasks;
using FinancialPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinancialPlatform.WebUI.Controllers
{
    [Authorize]
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

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
            // Tách vòng lặp JSON (Ngăn lỗi 500 Object Cycle của Entity Framework)
            if (order != null)
            {
                order.User = null;
                order.Asset = null;
            }

            return Ok(new { Message = "Đã nhận lệnh", OrderDetails = order });
        }
    }

    public class PlaceOrderRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(20)]
        public string Symbol { get; set; } = string.Empty;
        
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.RegularExpression("^(Buy|Sell)$", ErrorMessage = "Side must be either Buy or Sell.")]
        public string Side { get; set; } = "Buy";
        
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.RegularExpression("^(Market|Limit)$", ErrorMessage = "Type must be either Market or Limit.")]
        public string Type { get; set; } = "Market";
        
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.Range(0.000001, 1000000, ErrorMessage = "Amount must be greater than 0.")]
        public decimal Amount { get; set; }
        
        public decimal? LimitPrice { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
    }
}
