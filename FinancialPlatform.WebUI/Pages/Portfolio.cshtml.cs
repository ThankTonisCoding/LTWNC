using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FinancialPlatform.WebUI.Pages
{
    [Authorize]
    public class PortfolioModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
