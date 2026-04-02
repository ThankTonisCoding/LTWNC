using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FinancialPlatform.Core.Entities;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FinancialPlatform.WebUI.Pages
{
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExternalLoginModel(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public string ProviderDisplayName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Lỗi từ nhà cung cấp ngoài: {remoteError}");
                return Page();
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, "Lỗi khi tải thông tin đăng nhập từ bên ngoài.");
                return Page();
            }

            ProviderDisplayName = info.ProviderDisplayName ?? "Unknown";

            // Có tài khoản và đã liên kết thì cho đăng nhập luôn
            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
            if (signInResult.Succeeded)
            {
                return LocalRedirect(returnUrl);
            }

            // Nếu người dùng chưa có tài khoản -> Tự động tạo và tự động liên kết
            if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (string.IsNullOrEmpty(email))
                {
                    ModelState.AddModelError(string.Empty, "Email không hợp lệ từ tài khoản Google.");
                    return Page();
                }

                var user = await _userManager.FindByEmailAsync(email);
                
                if (user == null)
                {
                    user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
                    var createResult = await _userManager.CreateAsync(user);
                    if (!createResult.Succeeded)
                    {
                        foreach (var error in createResult.Errors)
                            ModelState.AddModelError(string.Empty, error.Description);
                        return Page();
                    }
                }

                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (addLoginResult.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: true, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }
            }

            ModelState.AddModelError(string.Empty, "Không thể hoàn tất quá trình liên kết tài khoản tự động.");
            return Page();
        }
    }
}
