using System.Globalization;
using System.Text;
using LTW09.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LTW09.Controllers;

public class HomeController(FlowerShopDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Products.AsNoTracking()
        .Include(p => p.Category).Where(p => p.IsActive && p.Category!.IsActive).ToListAsync());

    public async Task<IActionResult> New(string? tag, string? category, string? search)
    {
        ViewBag.BestSellers = await db.Products.AsNoTracking()
            .Where(p => p.IsActive && p.Category!.IsActive && p.StockQuantity > 0
                && p.Label != null && p.Label.Contains("Bán chạy"))
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt).Take(8).ToListAsync();
        ViewBag.CurrentTag = tag;
        ViewBag.CurrentCategory = category;
        ViewBag.CurrentSearch = search;
        ViewBag.Categories = await db.Categories.AsNoTracking().Where(c => c.IsActive).ToListAsync();
        var query = db.Products.AsNoTracking().Include(p => p.Category)
            .Where(p => p.IsActive && p.Category!.IsActive);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(p => p.Category!.Slug == category);
        // Chuẩn hóa tiếng Việt trong C# để tìm kiếm không phân biệt dấu trên SQLite.
        var products = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        var label = tag switch
        {
            "BestSeller" => "Bán chạy", "Sale" => "Giảm giá", "20-10" => "20/10",
            "Birthday" => "Sinh nhật", "Opening" => "Khai trương", _ => tag
        };
        if (!string.IsNullOrWhiteSpace(label))
            products = products.Where(p => Normalize(p.Label).Contains(Normalize(label))).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            products = products.Where(p => Normalize($"{p.Name} {p.Description} {p.Label}")
                .Contains(Normalize(search))).ToList();
        return View(products);
    }

    private static string Normalize(string? value) => new string((value ?? "").ToLowerInvariant()
        .Replace('đ', 'd').Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());

    public IActionResult Error() => View();
}
