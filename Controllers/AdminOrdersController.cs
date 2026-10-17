using LTW09.Data;
using LTW09.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LTW09.Controllers;

[Authorize(Roles = "Admin,Manager,Nhân viên")]
public class AdminOrdersController(FlowerShopDbContext db) : Controller
{
    private static readonly string[] Flow = ["Pending", "Confirmed", "Preparing", "Shipping", "Completed"];

    public async Task<IActionResult> Index(string? status, string? search)
    {
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentSearch = search;
        var query = db.Orders.AsNoTracking().Include(o => o.Customer).AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(o => o.OrderCode.Contains(search) || o.RecipientPhone.Contains(search)
                || o.RecipientName.Contains(search));
        }
        return View(await query.OrderByDescending(o => o.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.OrderId == id);
        return order == null ? NotFound() : View(order);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
    {
        if (!Flow.Contains(newStatus) && newStatus != "Cancelled") return BadRequest();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderId == orderId);
        if (order == null) return NotFound();
        if (order.Status == newStatus) return RedirectToAction(nameof(Details), new { id = orderId });
        if (order.Status is "Completed" or "Cancelled" ||
            (newStatus != "Cancelled" && Array.IndexOf(Flow, newStatus) <= Array.IndexOf(Flow, order.Status)))
        {
            TempData["Error"] = "Không thể chuyển lùi trạng thái hoặc sửa đơn đã hoàn tất/đã hủy.";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }
        if (newStatus == "Cancelled")
        {
            foreach (var item in order.Items.Where(i => i.ProductId.HasValue))
            {
                var product = await db.Products.FindAsync(item.ProductId!.Value);
                if (product == null) continue;
                product.StockQuantity = checked(product.StockQuantity + item.Quantity);
                product.UpdatedAt = DateTime.UtcNow;
                db.InventoryMovements.Add(new InventoryMovement
                {
                    ProductId = product.ProductId, QuantityChange = item.Quantity,
                    MovementType = "Return", ReferenceCode = order.OrderCode,
                    Note = "Hoàn kho do hủy đơn", CreatedAt = DateTime.UtcNow
                });
            }
        }
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["Success"] = newStatus == "Cancelled" ? "Đã hủy đơn và hoàn kho." : "Đã cập nhật trạng thái đơn.";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }
}
