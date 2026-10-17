using System.ComponentModel.DataAnnotations;
using LTW09.Data;
using LTW09.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LTW09.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class ProductsController(FlowerShopDbContext db, UserManager<IdentityUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> BestSellers()
    {
        var products = await db.Products.AsNoTracking().Where(p => p.IsActive && p.Category!.IsActive
            && p.StockQuantity > 0 && p.Label != null && p.Label.Contains("Bán chạy"))
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt).Take(8)
            .Select(p => new { p.ProductId, p.Name, p.Description, p.ImageUrl, p.Price, p.Label }).ToListAsync();
        return Json(products);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, int? stars, int page = 1)
    {
        if ((stars.HasValue && (stars.Value < 1 || stars.Value > 5)) || page < 1 || page > 10000) return BadRequest();
        var product = await db.Products.AsNoTracking().Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id && p.IsActive && p.Category!.IsActive);
        if (product == null) return NotFound();
        var query = db.ProductReviews.AsNoTracking().Where(r => r.ProductId == id);
        var groups = await query.GroupBy(r => r.Stars).Select(g => new { Stars = g.Key, Count = g.Count() }).ToListAsync();
        var count = groups.Sum(g => g.Count);
        var average = count == 0 ? (double?)null : groups.Sum(g => g.Stars * (double)g.Count) / count;
        if (stars.HasValue) query = query.Where(r => r.Stars == stars.Value);
        const int pageSize = 10;
        var filteredCount = await query.CountAsync();
        var reviews = await query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.ProductReviewId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new { r.ProductReviewId, r.DisplayName, r.Stars, r.Comment, r.CreatedAt }).ToListAsync();
        var userId = users.GetUserId(User);
        var myReview = userId == null ? null : await db.ProductReviews.AsNoTracking()
            .Where(r => r.ProductId == id && r.UserId == userId)
            .Select(r => new { r.Stars, r.Comment, r.DisplayName }).FirstOrDefaultAsync();
        return Json(new
        {
            product = new { product.ProductId, product.Name, product.Description, product.Price, product.ImageUrl,
                product.Label, product.StockQuantity, Category = product.Category?.Name },
            rating = new { count, average, counts = Enumerable.Range(1, 5).Select(s => new { stars = s, count = groups.FirstOrDefault(g => g.Stars == s)?.Count ?? 0 }) },
            reviews, filteredCount, page, pageSize, hasMore = page * pageSize < filteredCount,
            canReview = User.Identity?.IsAuthenticated == true, myReview
        });
    }

    [HttpGet]
    public async Task<IActionResult> Favorites(string? ids)
    {
        var keys = (ids ?? "").Split(',').Take(100).Select(s => int.TryParse(s, out var id) ? id : 0)
            .Where(id => id > 0).Distinct().ToArray();
        var products = await db.Products.AsNoTracking().Where(p => keys.Contains(p.ProductId) && p.IsActive && p.Category!.IsActive)
            .Select(p => new { p.ProductId, p.Name, p.ImageUrl, p.Price, p.StockQuantity }).ToListAsync();
        return Json(products);
    }

    public class ReviewInput
    {
        [Range(1, 5)] public int Stars { get; set; }
        [Required, StringLength(80)] public string DisplayName { get; set; } = "";
        [Required, StringLength(2000)] public string Comment { get; set; } = "";
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Review(int id, ReviewInput input)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "Nhập tên, nội dung tối đa 2.000 ký tự và số sao từ 1 đến 5." });
        if (string.IsNullOrWhiteSpace(input.Comment) || string.IsNullOrWhiteSpace(input.DisplayName))
            return BadRequest(new { message = "Tên và nội dung không được chỉ chứa khoảng trắng." });
        if (!await db.Products.AnyAsync(p => p.ProductId == id && p.IsActive && p.Category!.IsActive)) return NotFound();
        var userId = users.GetUserId(User);
        if (userId == null) return Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var review = await db.ProductReviews.FirstOrDefaultAsync(r => r.ProductId == id && r.UserId == userId);
        if (review == null)
        {
            review = new ProductReview { ProductId = id, UserId = userId };
            db.ProductReviews.Add(review);
        }
        review.Stars = input.Stars; review.DisplayName = input.DisplayName.Trim();
        review.Comment = input.Comment.Trim(); review.CreatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Json(new { message = "Đã lưu đánh giá của bạn." });
    }
}
