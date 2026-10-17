using LTW09.Data;
using LTW09.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LTW09.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController(UserManager<IdentityUser> users, AuthDbContext auth) : Controller
{
    private static readonly string[] Roles = ["Admin", "Nhân viên", "Khách hàng"];

    public async Task<IActionResult> Index()
    {
        ViewBag.AvailableRoles = Roles;
        var models = new List<AdminUserViewModel>();
        foreach (var user in await users.Users.OrderBy(u => u.Email).ToListAsync())
            models.Add(new AdminUserViewModel
            {
                Id = user.Id, Email = user.Email ?? "", UserName = user.UserName ?? "",
                PhoneNumber = user.PhoneNumber ?? "", Roles = (await users.GetRolesAsync(user)).ToList()
            });
        return View(models);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRole(string userId, string newRole)
    {
        if (!Roles.Contains(newRole)) return BadRequest();
        if (users.GetUserId(User) == userId && newRole != "Admin")
        {
            TempData["Error"] = "Bạn không thể tự bỏ quyền quản trị của mình.";
            return RedirectToAction(nameof(Index));
        }
        await using var transaction = await auth.Database.BeginTransactionAsync();
        var user = await users.FindByIdAsync(userId);
        if (user == null) return NotFound();
        var currentRoles = await users.GetRolesAsync(user);
        if (currentRoles.Contains("Admin") && newRole != "Admin" && (await users.GetUsersInRoleAsync("Admin")).Count <= 1)
        {
            TempData["Error"] = "Phải giữ ít nhất một quản trị viên.";
            return RedirectToAction(nameof(Index));
        }
        var remove = await users.RemoveFromRolesAsync(user, currentRoles);
        if (!remove.Succeeded) return RoleError(remove);
        var add = await users.AddToRoleAsync(user, newRole);
        if (!add.Succeeded) return RoleError(add);
        var stamp = await users.UpdateSecurityStampAsync(user);
        if (!stamp.Succeeded) return RoleError(stamp);
        await transaction.CommitAsync();
        TempData["Success"] = $"Đã chuyển {user.Email} sang vai trò {newRole}. Tài khoản cần đăng nhập lại.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RoleError(IdentityResult result)
    {
        TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Index));
    }
}
