using System.Threading.Tasks;
using FinancialPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialPlatform.WebUI.Controllers
{
    [Authorize(Roles = "Admin")] // Bảo mật API: Chỉ Admin
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminQueryService _adminQueryService;

        public AdminController(IAdminQueryService adminQueryService)
        {
            _adminQueryService = adminQueryService;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetDashboardMetrics()
        {
            var result = await _adminQueryService.GetDashboardMetricsAsync();
            return Ok(result);
        }
    }
}
