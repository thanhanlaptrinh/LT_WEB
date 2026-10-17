using LTW09.Data;
using LTW09.Models;
using LTW09.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LTW09.Controllers;

[Authorize(Roles = "Admin,Manager,Nhân viên")]
public class AdminProductsController(FlowerShopDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Products.AsNoTracking()
        .Include(p => p.Category).OrderByDescending(p => p.ProductId).ToListAsync());

    public async Task<IActionResult> Create()
    {
        await LoadCategories();
        return View(new ProductFormViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        await ValidateProduct(model);
        if (!ModelState.IsValid) { await LoadCategories(); return View(model); }
        await using var transaction = await db.Database.BeginTransactionAsync();
        var product = new Product { CreatedAt = DateTime.UtcNow };
        Apply(model, product);
        db.Products.Add(product);
        await db.SaveChangesAsync();
        if (product.StockQuantity != 0)
            AddMovement(product, product.StockQuantity, "Initial", "Tồn kho khi tạo sản phẩm");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Đã thêm sản phẩm.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.Products.FindAsync(id);
        if (p == null) return NotFound();
        await LoadCategories();
        return View(new ProductFormViewModel
        {
            ProductId = p.ProductId, CategoryId = p.CategoryId, Name = p.Name, Slug = p.Slug,
            Description = p.Description, Label = p.Label, Price = p.Price, StockQuantity = p.StockQuantity,
            ImageUrl = p.ImageUrl, IsActive = p.IsActive, IsFeatured = p.IsFeatured
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        if (id != model.ProductId) return BadRequest();
        await ValidateProduct(model);
        if (!ModelState.IsValid) { await LoadCategories(); return View(model); }
        await using var transaction = await db.Database.BeginTransactionAsync();
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound();
        var change = model.StockQuantity - product.StockQuantity;
        Apply(model, product);
        product.UpdatedAt = DateTime.UtcNow;
        if (change != 0) AddMovement(product, change, "Adjustment", "Điều chỉnh tồn kho tại trang sửa sản phẩm");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = "Đã lưu sản phẩm và cập nhật lịch sử kho.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.AsNoTracking().Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.ProductId == id);
        return product == null ? NotFound() : View(product);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null) return NotFound();
        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Đã ẩn sản phẩm, giữ nguyên lịch sử đơn hàng.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Inventory(int id)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
        if (product == null) return NotFound();
        ViewBag.Product = product;
        return View(await db.InventoryMovements.AsNoTracking().Where(m => m.ProductId == id)
            .OrderByDescending(m => m.CreatedAt).ToListAsync());
    }

    private async Task LoadCategories() => ViewBag.Categories = new SelectList(
        await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(), "CategoryId", "Name");

    private async Task ValidateProduct(ProductFormViewModel model)
    {
        if (!await db.Categories.AnyAsync(c => c.CategoryId == model.CategoryId))
            ModelState.AddModelError(nameof(model.CategoryId), "Danh mục không tồn tại.");
        if (await db.Products.AnyAsync(p => p.Slug == model.Slug && p.ProductId != model.ProductId))
            ModelState.AddModelError(nameof(model.Slug), "Slug đã được sử dụng. Vui lòng chọn slug khác.");
        if (!string.IsNullOrWhiteSpace(model.ImageUrl) &&
            !(model.ImageUrl.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase) ||
              Uri.TryCreate(model.ImageUrl, UriKind.Absolute, out var uri) && uri.Scheme == "https"))
            ModelState.AddModelError(nameof(model.ImageUrl), "Dùng ảnh /assets/... hoặc URL HTTPS.");
    }

    private static void Apply(ProductFormViewModel model, Product p)
    {
        p.CategoryId = model.CategoryId; p.Name = model.Name.Trim(); p.Slug = model.Slug.Trim();
        p.Description = model.Description; p.Label = model.Label; p.Price = model.Price;
        p.StockQuantity = model.StockQuantity;
        p.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? "/assets/tulips.jpg" : model.ImageUrl.Trim();
        p.IsActive = model.IsActive; p.IsFeatured = model.IsFeatured;
    }

    private void AddMovement(Product product, int quantity, string type, string note) =>
        db.InventoryMovements.Add(new InventoryMovement
        {
            ProductId = product.ProductId, QuantityChange = quantity, MovementType = type,
            ReferenceCode = $"PRODUCT-{product.ProductId}", Note = note, CreatedAt = DateTime.UtcNow
        });
}
