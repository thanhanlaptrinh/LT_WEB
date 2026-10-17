# Nhóm 7 — Đề tài 22: Quản lý cửa hàng hoa tươi

- Thành viên: Thành, Vinh, Vọng.
- Giảng viên hướng dẫn: Trần Việt Hùng.
- Website: Lá & Nụ — Fresh Flower Studio.
- ASP.NET Core MVC 8, EF Core 8, ASP.NET Identity, SQLite.

## 1. Bản cập nhật giao diện, banner và chi tiết hoa

- Font LaNu Sans (DejaVu Sans có đủ dấu tiếng Việt) được đóng gói tại wwwroot/fonts;
  mọi thành phần của layout chung dùng một họ font. Không tải font từ Google Fonts.
- Header gồm thông báo và menu được ghim khi cuộn; trên màn hình nhỏ dùng menu thu gọn.
- /Home/New có banner hoa đang bán, còn hàng và có nhãn “Bán chạy”. Tự lướt qua lại
  mỗi 4,5 giây; có nút trước/sau, chấm chọn và tạm dừng. Dừng khi rê chuột, thao tác
  bàn phím, ẩn tab hoặc mở bảng chi tiết. Tôn trọng thiết lập giảm chuyển động.
- Banner tự lấy lại danh sách sau mỗi 60 giây khi trang đang hiển thị và không được
  thao tác. Đổi nhãn/ảnh trong Quản lý hoa để cập nhật banner; tải lại trang để thấy ngay.
  Nếu không có hoa đủ điều kiện, hiện thông báo chờ cập nhật thay vì lấy hoa khác.
- Các nhãn sản phẩm hiển thị bằng ribbon đỏ ở góc trên trái; trái tim nằm góc trên phải.
- Gộp phần liên hệ cuối trang chủ và footer thành một footer dùng chung, giữ thương hiệu,
  số điện thoại, địa chỉ, giờ mở cửa và liên kết cần thiết.
- Nhấn ảnh, tên hoặc nền thẻ hoa để mở bảng chi tiết; có ảnh, mô tả, giá, tồn kho,
  số lượng, yêu thích, thêm giỏ và mua ngay. Phím Escape hoặc nút × đóng bảng.
- “Mua ngay” thêm số lượng đã chọn vào giỏ hiện có rồi chuyển đến Checkout. Giá/tồn
  cuối cùng luôn được server kiểm tra lại khi đặt đơn.
- Mục yêu thích trên menu lưu bằng localStorage của trình duyệt; giữ sau khi tải lại,
  không tự đồng bộ giữa các thiết bị hoặc tách theo tài khoản.
- Bảng chi tiết hiển thị điểm trung bình, số đánh giá theo từng mức sao, bộ lọc từ
  1 đến 5 sao và Tất cả. Nội dung dài có “Xem chi tiết đánh giá”; mỗi lần tải 10 đánh giá.
- Thành viên đăng nhập có thể gửi/cập nhật một đánh giá cho mỗi sản phẩm, điểm 1–5 sao.
  Đánh giá là cảm nhận thành viên, không có nhãn xác minh đã mua. Không tạo đánh giá mẫu.
  Chưa có dữ liệu sẽ hiện “Chưa có đánh giá”.
- Bảng ProductReviews được bổ sung vào cả file SQLite cũ khi khởi động. Không cần
  xóa App_Data/FlowerShop.db; không thay đổi schema của bảng sản phẩm/đơn/tài khoản cũ.

## 2. Chạy bản mới

1. Dừng cửa sổ chạy bản cũ bằng Ctrl+C hoặc nút Stop trong Visual Studio.
2. Giải nén ZIP vào **thư mục mới**, mở đúng LTW09.csproj trong thư mục mới.
3. Nếu muốn giữ dữ liệu đã tạo ở bản trước: khi ứng dụng đã dừng, chép thư mục
   App_Data từ bản trước sang thư mục mới (không xóa database cũ).
4. Mở Terminal tại thư mục có LTW09.csproj và chạy:

```powershell
dotnet restore
dotnet build
dotnet run --urls http://localhost:5080
```

Mở http://localhost:5080. Giữ cửa sổ chạy code mở. Sau khi nâng cấp nhấn Ctrl+F5
trong trình duyệt để tải lại CSS/JS. Nếu cổng 5080 đang bận, dừng bản cũ trước.
Có thể mở LTW09.csproj bằng Visual Studio 2022 rồi Ctrl+F5 để chạy ứng dụng.

Yêu cầu .NET SDK 8/Visual Studio có ASP.NET workload. Lần đầu restore cần mạng tải NuGet.
Không cần SQL Server/SSMS và không chạy file .sql để khởi tạo bản SQLite.
`npm start` chỉ mở HTML tĩnh; không chạy phần ASP.NET/database.

## 3. Đường dẫn cần kiểm tra

| Trang | URL | Quyền |
|---|---|---|
| Trang chủ | / | Mọi người |
| Bộ sưu tập | /Home/New | Mọi người |
| Đặt hàng | /Orders/Checkout | Mọi người |
| Tra cứu | /Orders/Lookup | Mọi người; nhập số điện thoại nhận hàng |
| Đơn hàng của tôi | /Orders/MyOrders | Đã đăng nhập |
| Đăng nhập | /Identity/Account/Login | Mọi người |
| Đăng ký | /Identity/Account/Register | Mọi người |
| Quản lý hoa | /AdminProducts | Admin/Manager/Nhân viên |
| Quản lý đơn | /AdminOrders | Admin/Manager/Nhân viên |
| Phân quyền | /AdminUsers | Admin |

Truy cập trang quản trị khi chưa đăng nhập sẽ chuyển đến Login. Khách hàng đã đăng
nhập nhưng thiếu quyền sẽ thấy AccessDenied; đây là hành vi phân quyền dự kiến.

## 4. Tài khoản mẫu

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@lanu.vn | Admin@123 |
| Nhân viên 1 | nhanvien1@lanu.vn | Staff@123 |
| Nhân viên 2 | nhanvien2@lanu.vn | Staff@123 |
| Khách hàng 1 | khach1@lanu.vn | Customer@123 |
| Khách hàng 2 | khach2@lanu.vn | Customer@123 |

Trang Login ở chế độ Development có phần mở rộng để điền nhanh 5 tài khoản này.
Đăng ký mới tự gán role Khách hàng; không cần cấu hình dịch vụ gửi email cho bản demo.
Mật khẩu tối thiểu 6 ký tự, có chữ thường; xác nhận mật khẩu phải khớp.
Mật khẩu được ASP.NET Identity băm; không lưu mật khẩu dạng văn bản trong database.

## 5. SQLite và dữ liệu

Hai file appsettings có cùng cấu hình:

```json
{
  "ConnectionStrings": {
    "FlowerShop": "Data Source=App_Data/FlowerShop.db;Foreign Keys=True"
  }
}
```

Database tự tạo khi chạy lần đầu. FlowerShopDbContext chứa cả bảng nghiệp vụ và
Identity; AuthDbContext dùng cùng file cho UserManager/RoleManager. Chỉ context
đầy đủ khởi tạo schema bằng EnsureCreated. Seed hai context dùng chung connection
và transaction, không bỏ qua lỗi khởi tạo.

Dữ liệu mẫu: 4 role (có Manager tương thích cũ), 5 tài khoản, 5 danh mục,
16 sản phẩm, 4 khách hàng, tồn kho ban đầu và 4 đơn mẫu. Không có dữ liệu thật từ
máy SQL Server của nhóm để nhập lại.

Tiền dùng decimal trong C#; cột SQLite INTEGER lưu giá trị × 100 để giữ 2 số lẻ.
Ví dụ 590.000 đồng lưu 59.000.000. EF tự chuyển khi đọc/ghi; cần lưu ý nếu xem/sửa SQL
trực tiếp bằng DB Browser for SQLite.

Đặt hàng tính lại giá/tổng từ database, kiểm tra tồn và trừ kho trong transaction;
không tin số tiền từ localStorage. Đơn từ 500.000 đồng miễn phí giao, dưới mức này
phí 30.000 đồng. Khi hủy đơn, hoàn kho một lần; không sửa tiếp đơn đã hủy/hoàn tất.
Các trạng thái thông thường chỉ chuyển tiến; có thể chuyển hủy trước khi hoàn tất.

Đơn của tài khoản gắn theo Customer.Email lấy từ tài khoản đã đăng nhập. Đơn guest
không được tự gán vào tài khoản khác bằng email nhập tay. Tra cứu guest dùng SĐT nhận.
Các email/chi tiết chuyển khoản trên giao diện phục vụ minh họa; không có tích hợp
cổng thanh toán hay tự gửi email xác nhận.

## 6. Kết quả kiểm tra và giới hạn

Đã kiểm tra:
- Cú pháp các script auth.js, flower-shop.js, storefront.js và script checkout.
- Bằng DOM giả lập: banner chạy qua lại, tạm dừng, hover, giảm chuyển động, trường hợp
  không có/chỉ có một slide; mở bảng chi tiết, lọc sao, yêu thích lưu lại, gửi đúng token
  trong form đánh giá, giới hạn số lượng theo tồn và chuyển sang Checkout khi mua ngay.
- Escape nội dung tên/đánh giá; giữ giỏ và khôi phục địa chỉ khi form đặt hàng lỗi.
- Chạy SQL bổ sung bảng bằng SQLite: chạy lặp không lỗi, giữ nguyên sản phẩm cũ,
  từ chối sao ngoài 1–5, từ chối đánh giá trùng sản phẩm/tài khoản và kiểm tra khóa ngoại.
- Đối chiếu các partial, liên kết tài nguyên và glyph tiếng Việt trong font được đóng gói.

**Chưa build/chạy ASP.NET và chưa kiểm tra hiển thị bằng trình duyệt thật tại môi trường
chỉnh sửa vì không có .NET SDK/browser runtime.** Các kiểm tra DOM/SQL riêng không thay
thế việc thử toàn bộ luồng HTTP/EF Core. Hãy chạy dotnet build và kiểm tra trên máy nhóm;
nếu lỗi, gửi nguyên thông báo ở Terminal hoặc Error List.

Có thể chạy `powershell -ExecutionPolicy Bypass -File .\KIEM_TRA_TRANG.ps1` khi ứng dụng
đang chạy để kiểm tra HTTP các trang công khai, file CSS/JS và font.

## 7. Thay đổi schema sau này

Không dùng migrations SQL Server trong LegacySqlServer với SQLite. Các migration cũ
đổi đuôi .cs.txt để không biên dịch. EnsureCreated không cập nhật schema đã tồn tại.
Khi thay đổi Models về sau, cần lập bộ migrations SQLite mới và phương án giữ dữ liệu.
Không xóa file .db có dữ liệu cần giữ. Dừng ứng dụng trước khi sao lưu database.

## 8. Deploy Docker / Render

Đã thêm Dockerfile, .dockerignore và cấu hình lưu khóa đăng nhập/health check.
Xem DEPLOY_RENDER.md để cấu hình Docker Web Service, PORT và Persistent Disk.
