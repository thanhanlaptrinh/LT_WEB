using System.ComponentModel.DataAnnotations;
using LTW09.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LTW09.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(UserManager<IdentityUser> users, SignInManager<IdentityUser> signIn, AuthDbContext auth) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public string ReturnUrl { get; set; } = "/";

    public class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), RegularExpression(@"^0\d{9}$", ErrorMessage = "Nhập số điện thoại 10 chữ số, bắt đầu bằng 0.")]
        public string PhoneNumber { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu từ 6 đến 100 ký tự."), DataType(DataType.Password)]
        public string Password { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu."), Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại chưa khớp."), DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = "";
    }

    public void OnGet(string? returnUrl = null) => ReturnUrl = LocalUrl(returnUrl);

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = LocalUrl(returnUrl);
        if (!ModelState.IsValid) return Page();
        await using var transaction = await auth.Database.BeginTransactionAsync();
        var email = Input.Email.Trim();
        if (await users.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError("Input.Email", "Email này đã có tài khoản.");
            return Page();
        }
        var user = new IdentityUser { UserName = email, Email = email, PhoneNumber = Input.PhoneNumber, EmailConfirmed = true };
        var result = await users.CreateAsync(user, Input.Password);
        if (result.Succeeded) result = await users.AddToRoleAsync(user, "Khách hàng");
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return Page();
        }
        await transaction.CommitAsync();
        await signIn.SignInAsync(user, isPersistent: false);
        return LocalRedirect(ReturnUrl);
    }

    private string LocalUrl(string? url) => !string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url) ? url : "/";
}
