using LTW09.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LTW09.Data;

public class FlowerShopDbContext : IdentityDbContext<IdentityUser>
{
    public FlowerShopDbContext(DbContextOptions<FlowerShopDbContext> options) : base(options) { }

    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ShopOrder> Orders => Set<ShopOrder>();
    public DbSet<ShopOrderItem> OrderItems => Set<ShopOrderItem>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Lưu tiền bằng số nguyên phần trăm đồng (VND × 100), giữ chính xác 2 số lẻ.
        // Không chuyển sang double vì có thể gây sai số tiền. C# vẫn dùng decimal.
        var moneyConverter = new ValueConverter<decimal, long>(
            value => decimal.ToInt64(decimal.Round(value * 100m, 0, MidpointRounding.AwayFromZero)),
            value => value / 100m);

        modelBuilder.Entity<ProductReview>(e =>
        {
            e.ToTable("ProductReviews", table => table.HasCheckConstraint("CK_ProductReviews_Stars", "\"Stars\" BETWEEN 1 AND 5"));
            e.HasKey(r => r.ProductReviewId);
            e.HasIndex(r => new { r.ProductId, r.UserId }).IsUnique();
            e.HasIndex(r => new { r.ProductId, r.Stars });
            e.HasOne(r => r.Product).WithMany().HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.Price).HasConversion(moneyConverter).HasColumnType("INTEGER");
            e.HasOne(x => x.Category).WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Category>().ToTable("Categories");
        modelBuilder.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => new { x.CategoryId, x.IsActive });
        modelBuilder.Entity<Product>().HasIndex(x => x.Name);
        modelBuilder.Entity<ShopOrder>().HasIndex(x => x.OrderCode).IsUnique();
        modelBuilder.Entity<ShopOrder>().HasIndex(x => new { x.Status, x.CreatedAt });
        modelBuilder.Entity<InventoryMovement>().HasIndex(x => new { x.ProductId, x.CreatedAt });
        modelBuilder.Entity<Customer>().ToTable("Customers");
        modelBuilder.Entity<ShopOrder>(e =>
        {
            e.ToTable("Orders");
            e.HasKey(x => x.OrderId);
            e.Property(x => x.Subtotal).HasConversion(moneyConverter).HasColumnType("INTEGER");
            e.Property(x => x.DeliveryFee).HasConversion(moneyConverter).HasColumnType("INTEGER");
            e.Property(x => x.TotalAmount).HasConversion(moneyConverter).HasColumnType("INTEGER");
            e.HasOne(x => x.Customer).WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<ShopOrderItem>(e =>
        {
            e.ToTable("OrderItems");
            e.HasKey(x => x.OrderItemId);
            e.Property(x => x.UnitPrice).HasConversion(moneyConverter).HasColumnType("INTEGER");
            e.Property(x => x.LineTotal).HasConversion(moneyConverter).HasColumnType("INTEGER");
            e.HasOne(x => x.Order).WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany()
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<InventoryMovement>(e =>
        {
            e.ToTable("InventoryMovements");
            e.HasKey(x => x.InventoryMovementId);
            e.HasOne(x => x.Product).WithMany()
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}