using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FinancialPlatform.Core.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;
using System;

namespace FinancialPlatform.WebUI.Pages
{
    public class OrderStateDto
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal? StopLoss { get; set; }
        public decimal? TakeProfit { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PortfolioDataDto
    {
        public string UserId { get; set; } = string.Empty;
        public decimal CashBalance { get; set; }
        public List<OrderStateDto> OpenOrders { get; set; } = new List<OrderStateDto>();
    }

    [Authorize]
    public class PortfolioModel : PageModel
    {
        private readonly IPortfolioQueryService _portfolioQueryService;

        public PortfolioModel(IPortfolioQueryService portfolioQueryService)
        {
            _portfolioQueryService = portfolioQueryService;
        }

        public decimal CashBalance { get; set; } = 0;
        public IEnumerable<OrderStateDto> OpenOrders { get; set; } = new List<OrderStateDto>();

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Login");

            var rawData = await _portfolioQueryService.GetPortfolioAsync(userId);
            if (rawData != null)
            {
                var json = JsonSerializer.Serialize(rawData);
                var parsed = JsonSerializer.Deserialize<PortfolioDataDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (parsed != null)
                {
                    CashBalance = parsed.CashBalance;
                    OpenOrders = parsed.OpenOrders ?? new List<OrderStateDto>();
                }
            }

            return Page();
        }
    }
}
