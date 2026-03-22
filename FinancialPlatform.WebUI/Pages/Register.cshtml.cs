using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FinancialPlatform.Core.Entities;
using FinancialPlatform.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FinancialPlatform.WebUI.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITradingService _tradingService;

        public RegisterModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITradingService tradingService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tradingService = tradingService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Email")]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            [DataType(DataType.Password)]
            [StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} - {1} ký tự.", MinimumLength = 6)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email };
                var result = await _userManager.CreateAsync(user, Input.Password);
                
                if (result.Succeeded)
                {
                    // Tự động cấp 10,000 USD tiền ảo ngay khi người dùng đăng ký
                    await _tradingService.InitializePaperWalletAsync(user.Id);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect("~/");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return Page();
        }
    }
}
