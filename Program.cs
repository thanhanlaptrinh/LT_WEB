using LTW09.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("FlowerShop")
    ?? throw new InvalidOperationException("Thiếu connection string 'FlowerShop'.");

// Giữ đường dẫn database cố định theo thư mục dự án, kể cả khi chạy từ thư mục khác.
var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
if (!string.IsNullOrWhiteSpace(sqliteConnection.DataSource) && sqliteConnection.DataSource != ":memory:")
{
    sqliteConnection.DataSource = Path.GetFullPath(sqliteConnection.DataSource, builder.Environment.ContentRootPath);
    Directory.CreateDirectory(Path.GetDirectoryName(sqliteConnection.DataSource)!);
}
sqliteConnection.ForeignKeys = true;
connectionString = sqliteConnection.ToString();

// Hai context dùng chung một file. FlowerShopDbContext chứa cả bảng nghiệp vụ và Identity.
// Chỉ FlowerShopDbContext khởi tạo schema, tránh EnsureCreated bỏ sót bảng ở context thứ hai.
// DbContext nghiệp vụ cửa hàng hoa tươi
builder.Services.AddDbContext<FlowerShopDbContext>(options =>
    options.UseSqlite(connectionString));

// DbContext riêng cho Identity
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlite(connectionString));

// Cấu hình Identity: xác thực + phân quyền 3 vai trò (Admin, Nhân viên, Khách hàng)
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AuthDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Tự kiểm tra anti-forgery cho các form POST của MVC. Razor Pages bật sẵn.
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute()));
// Đổi vai trò sẽ khiến phiên cũ được kiểm tra lại ngay ở request tiếp theo.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.Zero);
builder.Services.AddRazorPages();

var app = builder.Build();

// Tự động kiểm tra & Seed roles, tài khoản mẫu và dữ liệu hoa khi ứng dụng khởi động
// Dừng và báo lỗi rõ ràng nếu khởi tạo thất bại; không âm thầm chạy giao diện thiếu CSDL.
await DbSeeder.SeedAsync(app.Services);

// Pipeline xử lý yêu cầu HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
