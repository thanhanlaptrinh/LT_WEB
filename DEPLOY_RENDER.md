# Deploy nhóm 7 — Cửa hàng hoa tươi lên Render bằng Docker

## Các file mới/cập nhật

- Dockerfile: build nhiều giai đoạn SDK 8 → ASP.NET runtime 8.
- .dockerignore: bỏ thư mục build, database cục bộ, khóa đăng nhập và .env khỏi image.
- Program.cs: lưu khóa Data Protection theo DATA_PROTECTION_KEYS_PATH và thêm /healthz.
- .gitignore: không commit khóa đăng nhập và file .env.

Nếu đã có bản UI đang chạy trên máy, chỉ cần chép 4 file này vào đúng thư mục có
LTW09.csproj. Không ghi đè/xóa App_Data đang chứa dữ liệu của bạn.

## 1. Đẩy lên GitHub

Đặt Dockerfile và .dockerignore ngang cấp LTW09.csproj. Nếu cấu trúc repo hiện là:

```text
repo/
  LTW09.csproj
  Dockerfile
  .dockerignore
  Program.cs
  ...
```

thì dùng thư mục gốc repo làm Build Context. Commit các thay đổi:

```powershell
git add Dockerfile .dockerignore Program.cs .gitignore DEPLOY_RENDER.md
git commit -m "Add Docker deployment for Render"
git push origin main
```

Không commit file .db hoặc thư mục App_Data/keys. Image không mang theo database từ
máy của bạn; ứng dụng tạo dữ liệu mẫu nếu disk mới chưa có database.

## 2. Tạo Web Service trên Render

1. Render Dashboard → New → Web Service → kết nối repository GitHub.
2. Chọn nhánh chứa bản mới, thường là main.
3. Language / Runtime: Docker (không chọn Node dù repo có package.json).
4. Nếu LTW09.csproj và Dockerfile nằm ngay gốc repo:
   - Root Directory: để trống.
   - Dockerfile Path: ./Dockerfile.
   - Docker Build Context Directory: . (nếu giao diện có mục này).
5. Nếu repo chứa một thư mục con LTW09_SQLite_UI/ hoặc tương tự, đặt Root Directory
   bằng đúng tên thư mục con có LTW09.csproj và Dockerfile. Các path/context còn lại
   tính từ thư mục này, vẫn dùng ./Dockerfile và . .
6. Docker Command: để trống để Render chạy CMD trong Dockerfile.
7. Health Check Path: /healthz.
8. Tạo dịch vụ/deploy. Render tự build image; không cần nhập dotnet run làm Build Command.

Container đọc PORT do Render cấp và lắng nghe trên 0.0.0.0. Khi deploy xong,
mở URL https://...onrender.com do Render cung cấp, không dùng localhost.

## 3. SQLite: giữ dữ liệu sau restart/redeploy

Render mặc định dùng filesystem tạm thời. Nếu không gắn Persistent Disk, dữ liệu
SQLite và khóa đăng nhập trong container có thể mất khi restart/redeploy. Khi đó
lần khởi động mới tạo lại dữ liệu mẫu.

Muốn giữ tài khoản, đơn hàng, kho và đánh giá:

- Chọn Web Service trả phí có hỗ trợ Persistent Disk.
- Thêm Disk với mount path chính xác: /app/App_Data.
- Chọn dung lượng phù hợp, ví dụ 1 GB cho đồ án nhỏ.
- Chỉ chạy một instance cho cấu hình SQLite + disk này.

Không cần sửa chuỗi kết nối vì Dockerfile đã đặt:

```text
ConnectionStrings__FlowerShop=Data Source=/app/App_Data/FlowerShop.db;Foreign Keys=True
DATA_PROTECTION_KEYS_PATH=/app/App_Data/keys
```

Dữ liệu chỉ được giữ nếu nằm dưới mount path của Disk. Chỉ khai báo biến môi trường
hoặc thư mục trong Dockerfile không tự tạo Persistent Disk trên Render.

Có thể dùng Free cho demo ngắn và chấp nhận dữ liệu tạm; bản Free không hỗ trợ disk.
Nếu có cấu hình Environment cũ ConnectionStrings__FlowerShop trỏ SQL Server, hãy
xóa hoặc đổi về chuỗi SQLite trên, vì biến trên Render ghi đè ENV trong Dockerfile.

## 4. HTTPS và đăng nhập

Render cung cấp HTTPS ở proxy phía trước container. Dockerfile bật
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true và đặt ASPNETCORE_HTTPS_PORT=443
để ASP.NET nhận đúng scheme HTTPS, tạo redirect/cookie phù hợp.
Cấu hình forwarded headers này dành cho container đặt sau proxy của Render.

ASPNETCORE_ENVIRONMENT=Production: không hiện nút điền nhanh tài khoản demo ở trang
Login. Tài khoản mẫu vẫn như cũ; nhập email/mật khẩu bình thường.
Khóa cookie/anti-forgery được ghi vào App_Data/keys để giữ qua các lần restart khi
Disk được gắn. Favorites vẫn nằm trên trình duyệt: localhost và onrender.com là hai
origin khác nhau nên không tự chuyển yêu thích/giỏ hàng cũ sang domain deploy.

## 5. Thử Docker trên máy trước khi deploy

Yêu cầu Docker Desktop đang chạy. Tại thư mục có Dockerfile:

```powershell
docker build -t lanu-flower .
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development -v lanu-data:/app/App_Data lanu-flower
```

Mở http://localhost:8080. Dùng named volume lanu-data để giữ dữ liệu cục bộ.
Biến ASPNETCORE_ENVIRONMENT=Development ở lệnh thử local cho phép mở HTTP khi không có
TLS proxy. Trên Render giữ mặc định Production như trong Dockerfile.

Kiểm tra override PORT theo cách Render cấp:

```powershell
docker run --rm -p 10000:10000 -e PORT=10000 -e ASPNETCORE_ENVIRONMENT=Development -v lanu-data:/app/App_Data lanu-flower
```

Mở domain Render HTTPS khi deploy thật; container không tự cài chứng chỉ TLS.

## 6. Kiểm tra/lỗi thường gặp

- Không nhận cổng: Docker Command để trống; CMD tự đọc PORT.
- Build không thấy LTW09.csproj: Root Directory/Build Context đang chọn sai thư mục.
- Không mở trang chính nhưng healthz chạy: gửi log lỗi của request ở trang chính.
- SQLite unable to open database file: kiểm tra connection string/mount path và
  quyền ghi tại /app/App_Data; không mount đè thư mục /app chứa DLL của ứng dụng.
- Dữ liệu mất sau deploy: kiểm tra đã tạo Persistent Disk thật trên Render chưa.
- Login lỗi sau đổi phiên bản: kiểm tra keys nằm trong Disk, sau đó thử đăng nhập lại.

Môi trường chỉnh sửa chưa có Docker daemon và .NET SDK nên chưa build/chạy image
thực tế. Đã kiểm tra cấu trúc Dockerfile, lệnh khởi động theo PORT và các path.

Tài liệu chính thức:
- https://render.com/docs/docker
- https://render.com/docs/web-services#port-binding
- https://render.com/docs/disks
- https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-8.0
