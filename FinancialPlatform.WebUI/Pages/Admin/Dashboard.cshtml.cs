using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FinancialPlatform.Core.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Text.Json;

namespace FinancialPlatform.WebUI.Pages.Admin
{
    public class OrderStatsDto
    {
        public int Pending { get; set; }
        public int Filled { get; set; }
        public int Closed { get; set; }
    }

    public class RecentTradeDto
    {
        public string UserName { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class AdminDashboardDto
    {
        public int TotalUsers { get; set; }
        public decimal TotalCash { get; set; }
        public decimal TotalVolume { get; set; }
        public OrderStatsDto OrderStats { get; set; } = new OrderStatsDto();
        public List<RecentTradeDto> RecentTrades { get; set; } = new List<RecentTradeDto>();
    }

    // Bắt buộc phải là Admin mới được xem
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly IAdminQueryService _adminQueryService;

        public DashboardModel(IAdminQueryService adminQueryService)
        {
            _adminQueryService = adminQueryService;
        }

        public AdminDashboardDto Metrics { get; set; } = new AdminDashboardDto();

        public async Task OnGetAsync()
        {
            var rawData = await _adminQueryService.GetDashboardMetricsAsync();
            var json = JsonSerializer.Serialize(rawData);
            var parsed = JsonSerializer.Deserialize<AdminDashboardDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (parsed != null)
            {
                Metrics = parsed;
            }
        }
    }
}
