using LTW09.Data;
using LTW09.Models;
using LTW09.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LTW09.Controllers;

public class OrdersController(FlowerShopDbContext db, UserManager<IdentityUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var model = new CheckoutViewModel { DeliveryDate = DateTime.Today };
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await users.GetUserAsync(User);
            model.Email = user?.Email;
            model.RecipientPhone = user?.PhoneNumber ?? "";
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        model.DeliveryAddress = model.GetFullAddress();
        ModelState.Remove(nameof(model.DeliveryAddress));
        if (model.Items == null || model.Items.Count == 0 || model.Items.Count > 100)
            ModelState.AddModelError("", "Giỏ hoa phải có từ 1 đến 100 dòng sản phẩm.");
        if (model.PaymentMethod is not ("COD" or "BankTransfer"))
            ModelState.AddModelError(nameof(model.PaymentMethod), "Phương thức thanh toán không hợp lệ.");
        if (model.DeliveryDate.HasValue && model.DeliveryDate.Value.Date < DateTime.Today)
            ModelState.AddModelError(nameof(model.DeliveryDate), "Ngày giao không được ở quá khứ.");
        if (model.DeliveryAddress.Length > 500)
            ModelState.AddModelError(nameof(model.StreetAddress), "Địa chỉ hoàn chỉnh quá dài (tối đa 500 ký tự).");
        if (!ModelState.IsValid) return View(model);

        var user = User.Identity?.IsAuthenticated == true ? await users.GetUserAsync(User) : null;
        var quantities = model.Items!.GroupBy(i => i.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));
        if (quantities.Values.Any(q => q < 1 || q > 100))
        {
            ModelState.AddModelError("", "Mỗi sản phẩm được đặt từ 1 đến 100 bó.");
            return View(model);
        }
        // SQLite khóa ghi trong transaction; kiểm tra tồn và trừ kho cùng một transaction.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var ids = quantities.Keys.ToList();
        var products = await db.Products.Include(p => p.Category)
            .Where(p => ids.Contains(p.ProductId)).ToListAsync();
        foreach (var id in ids)
        {
            var p = products.FirstOrDefault(p => p.ProductId == id);
            if (p == null || !p.IsActive || p.Category?.IsActive != true)
                ModelState.AddModelError("", "Có sản phẩm đã ngừng bán. Vui lòng cập nhật giỏ hoa.");
            else if (p.StockQuantity < quantities[id])
                ModelState.AddModelError("", $"{p.Name} chỉ còn {p.StockQuantity} sản phẩm.");
        }
        if (!ModelState.IsValid) return View(model);

        Customer? customer = null;
        if (user != null)
            customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == user.Email);
        if (customer == null)
        {
            customer = new Customer
            {
                FullName = model.RecipientName.Trim(), Phone = model.RecipientPhone.Trim(),
                // Khách vãng lai không được gán đơn vào tài khoản khác bằng email nhập tay.
                Email = user?.Email, CreatedAt = DateTime.UtcNow
            };
            db.Customers.Add(customer);
        }
        var subtotal = products.Sum(p => p.Price * quantities[p.ProductId]);
        var fee = subtotal >= 500000m ? 0m : 30000m;
        var notes = new[] { model.IsAnonymousSender ? "Gửi ẩn danh" : null,
            model.DeliveryTimeSlot, model.CustomerNote }.Where(s => !string.IsNullOrWhiteSpace(s));
        var note = string.Join(" | ", notes);
        if (note.Length > 1000) note = note[..1000];
        var order = new ShopOrder
        {
            OrderCode = "LN" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Customer = customer, RecipientName = model.RecipientName.Trim(), RecipientPhone = model.RecipientPhone.Trim(),
            DeliveryAddress = model.DeliveryAddress, DeliveryDate = model.DeliveryDate,
            CardMessage = model.CardMessage, CustomerNote = note, PaymentMethod = model.PaymentMethod,
            Status = "Pending", Subtotal = subtotal, DeliveryFee = fee, TotalAmount = subtotal + fee,
            CreatedAt = DateTime.UtcNow
        };
        foreach (var p in products)
        {
            var quantity = quantities[p.ProductId];
            order.Items.Add(new ShopOrderItem
            {
                ProductId = p.ProductId, ProductName = p.Name, ImageUrl = p.ImageUrl,
                UnitPrice = p.Price, Quantity = quantity, LineTotal = p.Price * quantity
            });
            p.StockQuantity -= quantity;
            p.UpdatedAt = DateTime.UtcNow;
            db.InventoryMovements.Add(new InventoryMovement
            {
                ProductId = p.ProductId, QuantityChange = -quantity, MovementType = "Sale",
                ReferenceCode = order.OrderCode, Note = "Xuất kho khi đặt hoa", CreatedAt = DateTime.UtcNow
            });
        }
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        TempData["LastOrderId"] = order.OrderId;
        return RedirectToAction(nameof(Confirmation), new { id = order.OrderId });
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var order = await db.Orders.AsNoTracking().Include(o => o.Customer).Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderId == id);
        if (order == null) return NotFound();
        var user = User.Identity?.IsAuthenticated == true ? await users.GetUserAsync(User) : null;
        var ownsOrder = user?.Email != null && order.Customer?.Email == user.Email;
        var fromCheckout = TempData.Peek("LastOrderId") is int lastId && lastId == id;
        if (!fromCheckout && !ownsOrder && !User.IsInRole("Admin") && !User.IsInRole("Nhân viên"))
            return NotFound();
        return View(order);
    }

    [HttpGet]
    public IActionResult Lookup() => View();

    [HttpPost]
    public async Task<IActionResult> Lookup(string? phone, string? orderCode)
    {
        phone = new string((phone ?? "").Where(char.IsDigit).ToArray());
        orderCode = orderCode?.Trim().ToUpperInvariant();
        ViewBag.Phone = phone;
        ViewBag.OrderCode = orderCode;
        if (phone.Length != 10)
        {
            ViewBag.Error = "Vui lòng nhập số điện thoại 10 chữ số của người nhận.";
            return View();
        }
        var query = db.Orders.AsNoTracking().Include(o => o.Items).Where(o => o.RecipientPhone == phone);
        if (!string.IsNullOrWhiteSpace(orderCode)) query = query.Where(o => o.OrderCode == orderCode);
        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        if (orders.Count == 0)
        {
            ViewBag.Error = "Không tìm thấy đơn hàng khớp thông tin đã nhập.";
            return View();
        }
        return !string.IsNullOrWhiteSpace(orderCode)
            ? View("LookupResult", orders[0]) : View("LookupList", orders);
    }

    [Authorize]
    public async Task<IActionResult> MyOrders()
    {
        var user = await users.GetUserAsync(User);
        if (user?.Email == null) return Challenge();
        ViewBag.UserEmail = user.Email;
        return View(await db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.Customer != null && o.Customer.Email == user.Email)
            .OrderByDescending(o => o.CreatedAt).ToListAsync());
    }
}
