using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTW09.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LogoutModel(SignInManager<IdentityUser> signIn) : PageModel
{
    public void OnGet() { }
    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        await signIn.SignOutAsync();
        return LocalRedirect(!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "/");
    }
}
