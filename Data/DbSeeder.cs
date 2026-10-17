using LTW09.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LTW09.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var db = scope.ServiceProvider.GetRequiredService<FlowerShopDbContext>();

        // Context này chứa toàn bộ bảng nghiệp vụ + Identity trong cùng file SQLite.
        // EnsureCreated dành cho database mới; không dùng migrations SQL Server cũ.
        await db.Database.EnsureCreatedAsync();
        await SqliteSchemaUpgrade.ApplyAsync(db);
        var auth = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        auth.Database.SetDbConnection(db.Database.GetDbConnection());
        await using var transaction = await db.Database.BeginTransactionAsync();
        await auth.Database.UseTransactionAsync(transaction.GetDbTransaction());

        // 1. Seed Roles: Admin, Nhân viên, Khách hàng, Manager (tương thích cũ)
        var roles = new[] { "Admin", "Nhân viên", "Khách hàng", "Manager" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"Tạo vai trò {role}");
            }
        }

        // 2. Seed 5 tài khoản mẫu
        var defaultUsers = new[]
        {
            new { Email = "admin@lanu.vn", Password = "Admin@123", Roles = new[] { "Admin", "Manager" } },
            new { Email = "nhanvien1@lanu.vn", Password = "Staff@123", Roles = new[] { "Nhân viên" } },
            new { Email = "nhanvien2@lanu.vn", Password = "Staff@123", Roles = new[] { "Nhân viên" } },
            new { Email = "khach1@lanu.vn", Password = "Customer@123", Roles = new[] { "Khách hàng" } },
            new { Email = "khach2@lanu.vn", Password = "Customer@123", Roles = new[] { "Khách hàng" } },
        };

        foreach (var u in defaultUsers)
        {
            var user = await userManager.FindByEmailAsync(u.Email);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = u.Email,
                    Email = u.Email,
                    EmailConfirmed = true,
                    PhoneNumber = u.Email.Contains("khach1") ? "0901234567" :
                                 u.Email.Contains("khach2") ? "0918765432" :
                                 u.Email.Contains("nhanvien1") ? "0938111222" :
                                 u.Email.Contains("nhanvien2") ? "0938333444" : "0909999888"
                };

                var createResult = await userManager.CreateAsync(user, u.Password);
                EnsureSucceeded(createResult, $"Tạo tài khoản {u.Email}");
                foreach (var role in u.Roles)
                {
                    EnsureSucceeded(await userManager.AddToRoleAsync(user, role), $"Gán vai trò {role}");
                }
            }

        }

        // Tất cả thao tác seed cùng transaction để không để lại dữ liệu mẫu dang dở.
        await SeedBusinessDataAsync(db);
        await transaction.CommitAsync();
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"{operation}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }

    private static async Task SeedBusinessDataAsync(FlowerShopDbContext db)
    {
        // Kiểm tra xem database có bảng Categories chưa
        if (!await db.Categories.AnyAsync())
        {
            var categories = new List<Category>
            {
                new Category { Name = "Bó hoa tươi", Slug = "bouquet", Description = "Bó hoa nghệ thuật gói thủ công, trao gửi cảm xúc trọn vẹn.", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Hộp hoa cao cấp", Slug = "flower-box", Description = "Hộp hoa sang trọng kết hợp hoa nhập khẩu, thích hợp làm quà tặng lịch thiệp.", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Giỏ hoa chúc mừng", Slug = "flower-basket", Description = "Giỏ hoa tươi xinh xắn, sum vầy cho ngày sinh nhật và sự kiện đặc biệt.", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Hoa theo mùa & Cây cảnh", Slug = "seasonal", Description = "Những mùa hoa đẹp nhất trong năm tuyển chọn từ các nhà vườn danh tiếng.", IsActive = true, CreatedAt = DateTime.UtcNow },
                new Category { Name = "Bình hoa nghệ thuật", Slug = "vase", Description = "Bình hoa cắm sẵn phong cách hiện đại cho không gian sống và văn phòng.", IsActive = true, CreatedAt = DateTime.UtcNow }
            };

            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();
        }

        if (!await db.Products.AnyAsync())
        {
            var catBouquet = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "bouquet");
            var catBox = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "flower-box");
            var catBasket = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "flower-basket") ?? catBouquet;
            var catSeasonal = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "seasonal") ?? catBouquet;
            var catVase = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "vase") ?? catBox;

            var products = new List<Product>
            {
                new Product {
                    CategoryId = catBouquet!.CategoryId,
                    Name = "Nắng Đầu Mùa",
                    Slug = "nang-dau-mua",
                    Description = "Tulip hồng pastel phối baby Hà Lan và lá bạc thanh tao.",
                    Label = "Bán chạy",
                    Price = 590000m,
                    StockQuantity = 15,
                    ImageUrl = "/assets/tulips.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBouquet.CategoryId,
                    Name = "Thương Gửi Nàng",
                    Slug = "thuong-gui-nang",
                    Description = "Hồng Ohara kem phấn, cát tường trắng và thạch thảo tím mộng mơ.",
                    Label = "Lễ 20/10",
                    Price = 750000m,
                    StockQuantity = 12,
                    ImageUrl = "/assets/pink-bouquet.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBox!.CategoryId,
                    Name = "Mộng Mơ Tháng Tư",
                    Slug = "mong-mo-thang-tu",
                    Description = "Hộp hoa tròn nhung đỏ, phối hoa hồng ecuador và cẩm tú cầu lam biếc.",
                    Label = "Mới",
                    Price = 890000m,
                    StockQuantity = 8,
                    ImageUrl = "/assets/roses.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBouquet.CategoryId,
                    Name = "Một Ngày Rực Rỡ",
                    Slug = "mot-ngay-ruc-ro",
                    Description = "Đồng tiền vàng cam, hướng dương mặt trời và cúc tana dịu dàng.",
                    Label = "Sinh nhật",
                    Price = 490000m,
                    StockQuantity = 20,
                    ImageUrl = "/assets/mixed-flowers.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBox.CategoryId,
                    Name = "Dịu Dàng Sắc Xuân",
                    Slug = "diu-dang-sac-xuan",
                    Description = "Mẫu đơn hồng kem kết hợp lan hồ điệp mini và lá phụ cao cấp.",
                    Label = "Giảm giá 15%",
                    Price = 680000m,
                    StockQuantity = 6,
                    ImageUrl = "/assets/peony.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBouquet.CategoryId,
                    Name = "Lời Yêu Xanh",
                    Slug = "loi-yeu-xanh",
                    Description = "Tulip kép trắng tuyết, lan vũ nữ vàng và cỏ đồng nội bay bổng.",
                    Label = "Số lượng có hạn",
                    Price = 820000m,
                    StockQuantity = 4,
                    ImageUrl = "/assets/spring.jpg",
                    IsActive = true,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catSeasonal!.CategoryId,
                    Name = "Chớm Nở Yêu Thương",
                    Slug = "chom-no-yeu-thuong",
                    Description = "Cẩm chướng đỏ tươi phối cúc mẫu đơn và lá chanh Đà Lạt.",
                    Label = "Hoa theo mùa",
                    Price = 450000m,
                    StockQuantity = 14,
                    ImageUrl = "/assets/seasonal.jpg",
                    IsActive = true,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBox.CategoryId,
                    Name = "Bình Yên Nơi Phố Thị",
                    Slug = "binh-yen-noi-pho-thi",
                    Description = "Hoa tone trắng kem tinh khôi, hoa hồng mỡ gà và sao tím.",
                    Label = "Khai trương",
                    Price = 720000m,
                    StockQuantity = 9,
                    ImageUrl = "/assets/white-bouquet.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBasket!.CategoryId,
                    Name = "Hạnh Phúc Đong Đầy",
                    Slug = "hanh-phuc-dong-day",
                    Description = "Giỏ hoa gỗ vintage cắm tràn hoa hồng Spirit cam và lan mokara rực rỡ.",
                    Label = "Sinh nhật",
                    Price = 650000m,
                    StockQuantity = 7,
                    ImageUrl = "/assets/mixed-flowers.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catVase!.CategoryId,
                    Name = "Bình Hoa Thanh Khiết",
                    Slug = "binh-hoa-thanh-khiet",
                    Description = "Bình gốm mộc cắm hoa cúc mẫu đơn trắng, tú cầu và phi yến xanh lãng mạn.",
                    Label = "Mới",
                    Price = 950000m,
                    StockQuantity = 5,
                    ImageUrl = "/assets/white-bouquet.jpg",
                    IsActive = true,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBouquet.CategoryId,
                    Name = "Ngọt Ngào Tình Nồng",
                    Slug = "ngot-ngao-tinh-nong",
                    Description = "99 đóa hồng đỏ Pháp bó giấy đen viền vàng hoàng gia sang trọng.",
                    Label = "Lễ 20/10",
                    Price = 1450000m,
                    StockQuantity = 3, // Sắp hết
                    ImageUrl = "/assets/roses.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBasket.CategoryId,
                    Name = "Khai Trương Hồng Phát",
                    Slug = "khai-truong-hong-phat",
                    Description = "Kệ hoa / giỏ hoa tone đỏ vàng may mắn với hồng môn đỏ và thiên điểu.",
                    Label = "Khai trương",
                    Price = 1150000m,
                    StockQuantity = 8,
                    ImageUrl = "/assets/spring.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catSeasonal.CategoryId,
                    Name = "Thảo Nguyên Xanh Biếc",
                    Slug = "thao-nguyen-xanh-biec",
                    Description = "Cúc tana đồng nội kết hợp hoa baby trắng tinh khiết như mây trời.",
                    Label = "Bán chạy",
                    Price = 380000m,
                    StockQuantity = 25,
                    ImageUrl = "/assets/tulips.jpg",
                    IsActive = true,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBouquet.CategoryId,
                    Name = "Ánh Dương Ngày Mới",
                    Slug = "anh-duong-ngay-moi",
                    Description = "Bó hoa hướng dương 5 bông to phối cúc calimero và lá đuôi chồn.",
                    Label = "Sinh nhật",
                    Price = 420000m,
                    StockQuantity = 18,
                    ImageUrl = "/assets/mixed-flowers.jpg",
                    IsActive = true,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catBox.CategoryId,
                    Name = "Trái Tim Mùa Thu",
                    Slug = "trai-tim-mua-thu",
                    Description = "Hộp hoa trái tim xếp hoa hồng cam đào cappuccino và socola Ferrero.",
                    Label = "Giảm giá 20%",
                    Price = 790000m,
                    StockQuantity = 2, // Sắp hết
                    ImageUrl = "/assets/pink-bouquet.jpg",
                    IsActive = true,
                    IsFeatured = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Product {
                    CategoryId = catVase.CategoryId,
                    Name = "Lan Hồ Điệp Hoàng Gia",
                    Slug = "lan-ho-diep-hoang-gia",
                    Description = "Chậu lan hồ điệp 3 cành tím vân sang trọng, ý nghĩa phong thủy thịnh vượng.",
                    Label = "Khai trương",
                    Price = 1200000m,
                    StockQuantity = 5,
                    ImageUrl = "/assets/peony.jpg",
                    IsActive = true,
                    IsFeatured = false,
                    CreatedAt = DateTime.UtcNow
                }
            };

            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            // Thêm lịch sử nhập kho ban đầu
            foreach (var p in products)
            {
                db.InventoryMovements.Add(new InventoryMovement
                {
                    ProductId = p.ProductId,
                    QuantityChange = p.StockQuantity,
                    MovementType = "Initial",
                    ReferenceCode = "SEED-INIT",
                    Note = "Tồn kho ban đầu của hệ thống demo",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        // 4. Seed Khách hàng mẫu
        if (!await db.Customers.AnyAsync())
        {
            var customers = new List<Customer>
            {
                new Customer { FullName = "Nguyễn Thị Mai", Phone = "0901234567", Email = "khach1@lanu.vn", CreatedAt = DateTime.UtcNow.AddDays(-30) },
                new Customer { FullName = "Trần Hoàng Nam", Phone = "0918765432", Email = "khach2@lanu.vn", CreatedAt = DateTime.UtcNow.AddDays(-20) },
                new Customer { FullName = "Lê Minh Thư", Phone = "0933445566", Email = "minhthu@gmail.com", CreatedAt = DateTime.UtcNow.AddDays(-15) },
                new Customer { FullName = "Phạm Quốc Huy", Phone = "0977889900", Email = "quochuy@yahoo.com", CreatedAt = DateTime.UtcNow.AddDays(-10) }
            };
            db.Customers.AddRange(customers);
            await db.SaveChangesAsync();
        }

        // 5. Seed Đơn hàng mẫu đa dạng trạng thái
        if (!await db.Orders.AnyAsync())
        {
            var c1 = await db.Customers.FirstOrDefaultAsync(c => c.Phone == "0901234567");
            var c2 = await db.Customers.FirstOrDefaultAsync(c => c.Phone == "0918765432");
            var p1 = await db.Products.FirstOrDefaultAsync(p => p.Slug == "nang-dau-mua");
            var p2 = await db.Products.FirstOrDefaultAsync(p => p.Slug == "thuong-gui-nang");
            var p3 = await db.Products.FirstOrDefaultAsync(p => p.Slug == "mong-mo-thang-tu");
            var p4 = await db.Products.FirstOrDefaultAsync(p => p.Slug == "mot-ngay-ruc-ro");

            if (p1 != null && p2 != null)
            {
                // Đơn 1: Đã hoàn tất (Completed)
                var order1 = new ShopOrder
                {
                    OrderCode = "LN20261014001",
                    CustomerId = c1?.CustomerId,
                    RecipientName = "Nguyễn Thị Mai",
                    RecipientPhone = "0901234567",
                    DeliveryAddress = "Số 125 Phan Xích Long, Phường 2, Quận Phú Nhuận, TP. Hồ Chí Minh",
                    DeliveryDate = DateTime.Today.AddDays(-2),
                    CardMessage = "Chúc em yêu ngày 20/10 luôn xinh tươi và rạng rỡ như đóa tulip này nhé!",
                    CustomerNote = "[Giao sáng 09:00] Giao tận tay người nhận, gọi trước 10 phút.",
                    PaymentMethod = "BankTransfer",
                    Status = "Completed",
                    Subtotal = 590000m,
                    DeliveryFee = 0m,
                    TotalAmount = 590000m,
                    CreatedAt = DateTime.Now.AddDays(-2)
                };
                db.Orders.Add(order1);
                await db.SaveChangesAsync();

                db.OrderItems.Add(new ShopOrderItem
                {
                    OrderId = order1.OrderId,
                    ProductId = p1.ProductId,
                    ProductName = p1.Name,
                    ImageUrl = p1.ImageUrl,
                    UnitPrice = p1.Price,
                    Quantity = 1,
                    LineTotal = p1.Price
                });

                // Đơn 2: Đang giao hàng (Shipping)
                var order2 = new ShopOrder
                {
                    OrderCode = "LN20261015002",
                    CustomerId = c2?.CustomerId,
                    RecipientName = "Trần Thu Hà",
                    RecipientPhone = "0918765432",
                    DeliveryAddress = "Tòa nhà Bitexco, 2 Hải Triều, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                    DeliveryDate = DateTime.Today,
                    CardMessage = "Chúc mừng sinh nhật cô gái tháng 10! Tuổi mới luôn ngập tràn niềm vui.",
                    CustomerNote = "[Giao chiều 14:00] Gửi lễ tân tầng G bitexco giùm mình.",
                    PaymentMethod = "COD",
                    Status = "Shipping",
                    Subtotal = 750000m,
                    DeliveryFee = 0m,
                    TotalAmount = 750000m,
                    CreatedAt = DateTime.Now.AddHours(-4)
                };
                db.Orders.Add(order2);
                await db.SaveChangesAsync();

                db.OrderItems.Add(new ShopOrderItem
                {
                    OrderId = order2.OrderId,
                    ProductId = p2.ProductId,
                    ProductName = p2.Name,
                    ImageUrl = p2.ImageUrl,
                    UnitPrice = p2.Price,
                    Quantity = 1,
                    LineTotal = p2.Price
                });

                // Đơn 3: Đang chuẩn bị / cắm hoa (Preparing)
                if (p3 != null)
                {
                    var order3 = new ShopOrder
                    {
                        OrderCode = "LN20261016003",
                        CustomerId = c1?.CustomerId,
                        RecipientName = "Lê Thị Bích Ngọc",
                        RecipientPhone = "0933999888",
                        DeliveryAddress = "45 Lê Duẩn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                        DeliveryDate = DateTime.Today,
                        CardMessage = "Mừng khai trương chi nhánh mới phát tài phát lộc!",
                        CustomerNote = "[Giao chiều 16:00] Kèm thiệp in banner chúc mừng.",
                        PaymentMethod = "BankTransfer",
                        Status = "Preparing",
                        Subtotal = 890000m,
                        DeliveryFee = 0m,
                        TotalAmount = 890000m,
                        CreatedAt = DateTime.Now.AddHours(-2)
                    };
                    db.Orders.Add(order3);
                    await db.SaveChangesAsync();

                    db.OrderItems.Add(new ShopOrderItem
                    {
                        OrderId = order3.OrderId,
                        ProductId = p3.ProductId,
                        ProductName = p3.Name,
                        ImageUrl = p3.ImageUrl,
                        UnitPrice = p3.Price,
                        Quantity = 1,
                        LineTotal = p3.Price
                    });
                }

                // Đơn 4: Chờ xác nhận (Pending)
                if (p4 != null)
                {
                    var order4 = new ShopOrder
                    {
                        OrderCode = "LN20261016004",
                        CustomerId = null, // Guest checkout
                        RecipientName = "Hoàng Hải Yến",
                        RecipientPhone = "0987654321",
                        DeliveryAddress = "88 Nguyễn Thị Minh Khai, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh",
                        DeliveryDate = DateTime.Today.AddDays(1),
                        CardMessage = "Chúc em ngày mới luôn rạng rỡ yêu đời!",
                        CustomerNote = "[Giao sáng 08:30] Khách yêu cầu giao sớm trước 9h.",
                        PaymentMethod = "COD",
                        Status = "Pending",
                        Subtotal = 490000m,
                        DeliveryFee = 30000m,
                        TotalAmount = 520000m,
                        CreatedAt = DateTime.Now.AddMinutes(-30)
                    };
                    db.Orders.Add(order4);
                    await db.SaveChangesAsync();

                    db.OrderItems.Add(new ShopOrderItem
                    {
                        OrderId = order4.OrderId,
                        ProductId = p4.ProductId,
                        ProductName = p4.Name,
                        ImageUrl = p4.ImageUrl,
                        UnitPrice = p4.Price,
                        Quantity = 1,
                        LineTotal = p4.Price
                    });
                }

                await db.SaveChangesAsync();
            }
        }

        // Seed thêm 22 sản phẩm mới (chỉ insert nếu chưa có theo Slug)
        await SeedNewProductsAsync(db);
    }

    private static async Task SeedNewProductsAsync(FlowerShopDbContext db)
    {
        var catBouquet = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "bouquet");
        var catBox = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "flower-box");
        var catSeasonal = await db.Categories.FirstOrDefaultAsync(c => c.Slug == "seasonal") ?? catBouquet;
        if (catBouquet == null || catBox == null) return; // Categories chưa được tạo

        // Danh sách sản phẩm mới cần seed (slug -> dữ liệu)
        var newProducts = new[]
        {
            new { CategoryId = catBouquet.CategoryId, Name = "Sắc Hồng Mùa Xuân", Slug = "sac-hong-mua-xuan",
                  Description = "Hồng Ecuador phối lá bạc, gói giấy craft tự nhiên.", Label = "Được yêu thích",
                  Price = 780000m, Stock = 10, Image = "/assets/sac-hong-mua-xuan.jpg", Featured = false },
            new { CategoryId = catBouquet.CategoryId, Name = "Gửi Em Ngàn Sao", Slug = "gui-em-ngan-sao",
                  Description = "Hồng đỏ và baby trắng, thông điệp tình yêu ngập tràn.", Label = "Bán chạy",
                  Price = 850000m, Stock = 7, Image = "/assets/gui-em-ngan-sao.jpg", Featured = true },
            new { CategoryId = catSeasonal!.CategoryId, Name = "Nắng Vàng Trong Veo", Slug = "nang-vang-trong-veo",
                  Description = "Hoa đồng tiền vàng phối cúc tana, sắc hè tươi sáng.", Label = "Hoa theo mùa",
                  Price = 520000m, Stock = 11, Image = "/assets/nang-vang-trong-veo.jpg", Featured = false },
            new { CategoryId = catBouquet.CategoryId, Name = "Hồng Phai Mộng Mơ", Slug = "hong-phai-mong-mo",
                  Description = "Hồng pastel phối cẩm chướng, nhẹ nhàng và tinh tế.", Label = "Mới",
                  Price = 690000m, Stock = 8, Image = "/assets/hong-phai-mong-mo.jpg", Featured = false },
            new { CategoryId = catBox.CategoryId, Name = "Hộp Hoa Thanh Xuân", Slug = "hop-hoa-thanh-xuan",
                  Description = "Hộp hoa hồng kem phối lá xanh, thiết kế sang trọng.", Label = "Thiết kế riêng",
                  Price = 980000m, Stock = 5, Image = "/assets/hop-hoa-thanh-xuan.jpg", Featured = true },
            new { CategoryId = catBox.CategoryId, Name = "Mùa Thương Gửi Em", Slug = "mua-thuong-gui-em",
                  Description = "Hồng phấn và cát tường trong hộp quà tinh tế.", Label = "Gói thủ công",
                  Price = 920000m, Stock = 6, Image = "/assets/mua-thuong-gui-em.jpg", Featured = false },
            new { CategoryId = catSeasonal.CategoryId, Name = "Thoảng Hương Tinh Khôi", Slug = "thoang-huong-tinh-khoi",
                  Description = "Hoa cúc trắng theo mùa, tinh khôi và thanh lịch.", Label = "Hoa theo mùa",
                  Price = 430000m, Stock = 13, Image = "/assets/thoang-huong-tinh-khoi.jpg", Featured = false },
            new { CategoryId = catBouquet.CategoryId, Name = "Ban Mai Dịu Êm", Slug = "ban-mai-diu-em",
                  Description = "Tulip hồng phối cúc nhỏ, gợi cảm giác bình yên buổi sáng.", Label = "Mới",
                  Price = 760000m, Stock = 9, Image = "/assets/ban-mai-diu-em.jpg", Featured = false },
            new { CategoryId = catBouquet.CategoryId, Name = "Lời Thương Ngọt Ngào", Slug = "loi-thuong-ngot-ngao",
                  Description = "Hồng kem phối hoa baby, nhẹ nhàng như lời thì thầm.", Label = "Được yêu thích",
                  Price = 640000m, Stock = 12, Image = "/assets/loi-thuong-ngot-ngao.jpg", Featured = false },
            new { CategoryId = catSeasonal.CategoryId, Name = "Gió Hè Bên Em", Slug = "gio-he-ben-em",
                  Description = "Hoa phối sắc vàng và cam, hương sắc mùa hè rực rỡ.", Label = "Hoa theo mùa",
                  Price = 560000m, Stock = 10, Image = "/assets/gio-he-ben-em.jpg", Featured = false },
            new { CategoryId = catBox.CategoryId, Name = "Vòng Tay Yêu Thương", Slug = "vong-tay-yeu-thuong",
                  Description = "Hồng đỏ trong hộp quà sang trọng, trao gửi yêu thương.", Label = "Bán chạy",
                  Price = 1150000m, Stock = 4, Image = "/assets/vong-tay-yeu-thuong.jpg", Featured = true },
            new { CategoryId = catBouquet.CategoryId, Name = "Hồng Chạm Nhẹ Nhàng", Slug = "hong-cham-nhe-nhang",
                  Description = "Hồng phấn phối lá eucalyptus, dịu dàng và thơm mát.", Label = "Gói thủ công",
                  Price = 730000m, Stock = 8, Image = "/assets/hong-cham-nhe-nhang.jpg", Featured = false },
            new { CategoryId = catBouquet.CategoryId, Name = "Hoa Cưới An Yên", Slug = "hoa-cuoi-an-yen",
                  Description = "Hoa trắng xanh thanh lịch, hoàn hảo cho ngày trọng đại.", Label = "Thiết kế riêng",
                  Price = 1250000m, Stock = 3, Image = "/assets/hoa-cuoi-an-yen.jpg", Featured = true },
            new { CategoryId = catBox.CategoryId, Name = "Hộp Hoa Chúc Mừng", Slug = "hop-hoa-chuc-mung",
                  Description = "Hoa phối màu tươi sáng trong hộp, mang niềm vui đến mọi dịp.", Label = "Chúc mừng",
                  Price = 890000m, Stock = 6, Image = "/assets/hop-hoa-chuc-mung.jpg", Featured = false },
            new { CategoryId = catSeasonal.CategoryId, Name = "Sắc Màu Hạnh Phúc", Slug = "sac-mau-hanh-phuc",
                  Description = "Hoa đồng tiền và cúc nhiều màu, rực rỡ và tươi vui.", Label = "Hoa theo mùa",
                  Price = 490000m, Stock = 14, Image = "/assets/sac-mau-hanh-phuc.jpg", Featured = false },
            new { CategoryId = catBouquet.CategoryId, Name = "Tình Yêu Đầu", Slug = "tinh-yeu-dau",
                  Description = "Tulip đỏ hồng gói giấy kraft, ký ức ngọt ngào tình yêu đầu.", Label = "Bán chạy",
                  Price = 960000m, Stock = 5, Image = "/assets/tinh-yeu-dau.jpg", Featured = true },
            new { CategoryId = catBouquet.CategoryId, Name = "Bản Tình Ca Hồng", Slug = "ban-tinh-ca-hong",
                  Description = "Hồng garden phối cát tường, bản tình ca bất tận.", Label = "Được yêu thích",
                  Price = 880000m, Stock = 7, Image = "/assets/ban-tinh-ca-hong.jpg", Featured = false },
            new { CategoryId = catSeasonal.CategoryId, Name = "Tháng Năm Rực Rỡ", Slug = "thang-nam-ruc-ro",
                  Description = "Hoa mùa hè sắc vàng cam, rực rỡ như tháng năm tuổi trẻ.", Label = "Hoa theo mùa",
                  Price = 510000m, Stock = 10, Image = "/assets/thang-nam-ruc-ro.jpg", Featured = false },
            new { CategoryId = catBox.CategoryId, Name = "Nắng Ôm Mùa Hoa", Slug = "nang-om-mua-hoa",
                  Description = "Hồng vàng phối cẩm tú cầu, ấm áp như nắng ban mai.", Label = "Mới",
                  Price = 1050000m, Stock = 4, Image = "/assets/nang-om-mua-hoa.jpg", Featured = true },
            new { CategoryId = catBouquet.CategoryId, Name = "Gửi Người Thương", Slug = "gui-nguoi-thuong",
                  Description = "Hoa trắng xanh gói giấy tự nhiên, gửi trọn yêu thương.", Label = "Gói thủ công",
                  Price = 790000m, Stock = 8, Image = "/assets/gui-nguoi-thuong.jpg", Featured = false },
            new { CategoryId = catSeasonal.CategoryId, Name = "Điều Ước Bình Yên", Slug = "dieu-uoc-binh-yen",
                  Description = "Hoa cúc trắng và lá xanh, điều ước về cuộc sống bình yên.", Label = "Hoa theo mùa",
                  Price = 470000m, Stock = 12, Image = "/assets/dieu-uoc-binh-yen.jpg", Featured = false },
            new { CategoryId = catBox.CategoryId, Name = "Hộp Hoa Lung Linh", Slug = "hop-hoa-lung-linh",
                  Description = "Hồng pastel và hoa điểm nhỏ, lung linh như giấc mơ.", Label = "Thiết kế riêng",
                  Price = 990000m, Stock = 5, Image = "/assets/hop-hoa-lung-linh.jpg", Featured = false },
        };

        var toInsert = new List<Product>();
        foreach (var np in newProducts)
        {
            // Chỉ insert nếu chưa có sản phẩm với slug này
            if (!await db.Products.AnyAsync(p => p.Slug == np.Slug))
            {
                toInsert.Add(new Product
                {
                    CategoryId = np.CategoryId,
                    Name = np.Name,
                    Slug = np.Slug,
                    Description = np.Description,
                    Label = np.Label,
                    Price = np.Price,
                    StockQuantity = np.Stock,
                    ImageUrl = np.Image,
                    IsActive = true,
                    IsFeatured = np.Featured,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        if (toInsert.Count > 0)
        {
            db.Products.AddRange(toInsert);
            await db.SaveChangesAsync();

            // Thêm lịch sử nhập kho cho sản phẩm mới
            foreach (var p in toInsert)
            {
                db.InventoryMovements.Add(new InventoryMovement
                {
                    ProductId = p.ProductId,
                    QuantityChange = p.StockQuantity,
                    MovementType = "Initial",
                    ReferenceCode = "SEED-NEW",
                    Note = "Tồn kho ban đầu — sản phẩm bổ sung",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }
    }
}
