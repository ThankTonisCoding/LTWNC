using System.Security.Claims;
using System.Threading.Tasks;
using FinancialPlatform.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialPlatform.WebUI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly IPortfolioQueryService _portfolioQueryService;

        public PortfolioController(IPortfolioQueryService portfolioQueryService)
        {
            _portfolioQueryService = portfolioQueryService;
        }

        // Lấy danh sách Lệnh đang mở và Số dư ví
        [HttpGet("current")]
        public async Task<IActionResult> GetPortfolio()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _portfolioQueryService.GetPortfolioAsync(userId);
            if (result == null) 
                return NotFound(new { Message = "User chưa được cấp ví ảo." });

            return Ok(result);
        }
    }
}
