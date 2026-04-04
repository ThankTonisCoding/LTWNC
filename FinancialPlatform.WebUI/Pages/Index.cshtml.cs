using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FinancialPlatform.Core.Interfaces;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FinancialPlatform.WebUI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ITradingService _tradingService;

        public IndexModel(ILogger<IndexModel> logger, ITradingService tradingService)
        {
            _logger = logger;
            _tradingService = tradingService;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostPlaceOrderAsync(string symbol, string side, decimal amount)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                TempData["OrderFeedbackTitle"] = "Lỗi xác thực";
                TempData["OrderFeedbackMessage"] = "Vui lòng đăng nhập lại để giao dịch.";
                TempData["OrderFeedbackType"] = "error";
                return RedirectToPage();
            }

            if (amount <= 0)
            {
                TempData["OrderFeedbackTitle"] = "Lỗi khối lượng";
                TempData["OrderFeedbackMessage"] = "Số lượng phải lớn hơn 0.";
                TempData["OrderFeedbackType"] = "error";
                return RedirectToPage();
            }

            try
            {
                var order = await _tradingService.PlaceOrderAsync(
                    userId, 
                    symbol ?? "BTCUSDT", 
                    side, 
                    "Market", 
                    amount, 
                    null, 
                    null, 
                    null
                );

                if (order != null)
                {
                    TempData["OrderFeedbackTitle"] = "Thành công";
                    TempData["OrderFeedbackMessage"] = $"Khớp lệnh {side} {amount} {symbol} (Giá: {order.Price:C})";
                    TempData["OrderFeedbackType"] = "success";
                }
                else
                {
                    TempData["OrderFeedbackTitle"] = "Lỗi đặt lệnh";
                    TempData["OrderFeedbackMessage"] = "Lỗi xử lý hệ thống. Vui lòng thử lại.";
                    TempData["OrderFeedbackType"] = "error";
                }
            }
            catch (System.Exception)
            {
                TempData["OrderFeedbackTitle"] = "Thất bại";
                TempData["OrderFeedbackMessage"] = "Không đủ số dư hoặc tài sản gặp lỗi.";
                TempData["OrderFeedbackType"] = "error";
            }

            // Dùng hash để cuộn xuống thẳng chỗ nhập lệnh
            return RedirectToPage(new { symbol });
        }
    }
}
