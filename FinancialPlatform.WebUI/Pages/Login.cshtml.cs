using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FinancialPlatform.Core.Entities;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FinancialPlatform.WebUI.Pages
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LoginModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Email")]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? Url.Content("~/");
        }

        public IActionResult OnPostGoogle(string? returnUrl = null)
        {
            var redirectUrl = Url.Page("/ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, isPersistent: true, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return LocalRedirect(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không chính xác.");
                    return Page();
                }
            }
            return Page();
        }
    }
}
