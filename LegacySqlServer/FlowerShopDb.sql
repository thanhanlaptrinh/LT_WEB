/*
===================================================================================
  DỰ ÁN TỐT NGHIỆP / ĐỒ ÁN LẬP TRÌNH WEB: CỬA HÀNG HOA TƯƠI LÁ & NỤ
  FILE SQL TỔNG HỢP TOÀN BỘ CƠ SỞ DỮ LIỆU & DỮ LIỆU MẪU (DATABASE BOOTSTRAP)
  Hệ quản trị CSDL: Microsoft SQL Server 2019+ / SQL Express / LocalDB
  Tính năng:
    1. Tạo Database [FlowerShopDb] nếu chưa có
    2. Tạo Bảng nghiệp vụ: Categories, Products, Customers, Orders, OrderItems, InventoryMovements
    3. Tạo Bảng phân quyền ASP.NET Identity: AspNetRoles, AspNetUsers, AspNetUserRoles, v.v.
    4. Khởi tạo 5 nhóm danh mục hoa tươi nghệ thuật
    5. Khởi tạo 16 sản phẩm hoa phong phú với đầy đủ Tag nghiệp vụ (Mới, Bán chạy, 20/10, Sinh nhật, Khai trương, Giảm giá)
    6. Khởi tạo Khách hàng mẫu & Lịch sử xuất nhập kho ban đầu
    7. Khởi tạo 5 đơn hàng mẫu đa dạng trạng thái (Pending, Preparing, Shipping, Completed, Cancelled)
    8. Khởi tạo 3 Role chính: [Admin], [Nhân viên], [Khách hàng] (+ role dự phòng [Manager])
    9. Khởi tạo 5 tài khoản mẫu kèm PasswordHash chuẩn hóa ASP.NET Core:
       - Quản trị viên : admin@lanu.vn     | Mật khẩu: Admin@123    (Role: Admin, Manager)
       - Nhân viên 1   : nhanvien1@lanu.vn | Mật khẩu: Staff@123    (Role: Nhân viên)
       - Nhân viên 2   : nhanvien2@lanu.vn | Mật khẩu: Staff@123    (Role: Nhân viên)
       - Khách hàng 1  : khach1@lanu.vn    | Mật khẩu: Customer@123 (Role: Khách hàng)
       - Khách hàng 2  : khach2@lanu.vn    | Mật khẩu: Customer@123 (Role: Khách hàng)
===================================================================================
*/

IF DB_ID(N'FlowerShopDb') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [FlowerShopDb]');
END;
GO

USE [FlowerShopDb];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- ===================================================================
-- 1. TẠO CÁC BẢNG QUẢN LÝ TÀI KHOẢN VÀ PHÂN QUYỀN (ASP.NET IDENTITY)
-- ===================================================================

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] NVARCHAR(150) NOT NULL,
        [ProductVersion] NVARCHAR(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles (
        Id               NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
        Name             NVARCHAR(256) NULL,
        NormalizedName   NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL
    );
    CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers (
        Id                   NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
        UserName             NVARCHAR(256) NULL,
        NormalizedUserName   NVARCHAR(256) NULL,
        Email                NVARCHAR(256) NULL,
        NormalizedEmail      NVARCHAR(256) NULL,
        EmailConfirmed       BIT NOT NULL,
        PasswordHash         NVARCHAR(MAX) NULL,
        SecurityStamp        NVARCHAR(MAX) NULL,
        ConcurrencyStamp     NVARCHAR(MAX) NULL,
        PhoneNumber          NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL,
        TwoFactorEnabled     BIT NOT NULL,
        LockoutEnd           DATETIMEOFFSET NULL,
        LockoutEnabled       BIT NOT NULL,
        AccessFailedCount    INT NOT NULL
    );
    CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
    CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles (
        UserId NVARCHAR(450) NOT NULL,
        RoleId NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles (RoleId);
END;
GO

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims (
        Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY,
        RoleId     NVARCHAR(450) NOT NULL,
        ClaimType  NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims (RoleId);
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims (
        Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AspNetUserClaims PRIMARY KEY,
        UserId     NVARCHAR(450) NOT NULL,
        ClaimType  NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims (UserId);
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins (
        LoginProvider       NVARCHAR(128) NOT NULL,
        ProviderKey         NVARCHAR(128) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId              NVARCHAR(450) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins (UserId);
END;
GO

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens (
        UserId        NVARCHAR(450) NOT NULL,
        LoginProvider NVARCHAR(128) NOT NULL,
        Name          NVARCHAR(128) NOT NULL,
        Value         NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
    );
END;
GO

-- ===================================================================
-- 2. TẠO CÁC BẢNG NGHIỆP VỤ CỬA HÀNG HOA LÁ & NỤ
-- ===================================================================

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        CategoryId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
        Name         NVARCHAR(100) NOT NULL,
        Slug         VARCHAR(100) NOT NULL,
        Description  NVARCHAR(500) NULL,
        IsActive     BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
        CreatedAt    DATETIME2(0) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Categories_Name UNIQUE (Name),
        CONSTRAINT UQ_Categories_Slug UNIQUE (Slug)
    );
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        CategoryId    INT NOT NULL,
        Name          NVARCHAR(160) NOT NULL,
        Slug          VARCHAR(180) NOT NULL,
        Description   NVARCHAR(1000) NULL,
        Label         NVARCHAR(80) NULL,
        Price         DECIMAL(18,2) NOT NULL,
        StockQuantity INT NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT (0),
        ImageUrl      NVARCHAR(500) NOT NULL,
        IsActive      BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        IsFeatured    BIT NOT NULL CONSTRAINT DF_Products_IsFeatured DEFAULT (0),
        CreatedAt     DATETIME2(0) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2(0) NULL,
        CONSTRAINT UQ_Products_Slug UNIQUE (Slug),
        CONSTRAINT CK_Products_Price CHECK (Price >= 0),
        CONSTRAINT CK_Products_StockQuantity CHECK (StockQuantity >= 0),
        CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
            REFERENCES dbo.Categories(CategoryId) ON DELETE NO ACTION
    );
END;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
        FullName   NVARCHAR(160) NOT NULL,
        Phone      NVARCHAR(30) NOT NULL,
        Email      NVARCHAR(254) NULL,
        CreatedAt  DATETIME2(0) NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders
    (
        OrderId          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
        OrderCode        VARCHAR(30) NOT NULL,
        CustomerId       INT NULL,
        RecipientName    NVARCHAR(160) NOT NULL,
        RecipientPhone   NVARCHAR(30) NOT NULL,
        DeliveryAddress  NVARCHAR(500) NOT NULL,
        DeliveryDate     DATE NULL,
        CardMessage      NVARCHAR(500) NULL,
        CustomerNote     NVARCHAR(1000) NULL,
        PaymentMethod    VARCHAR(30) NOT NULL CONSTRAINT DF_Orders_PaymentMethod DEFAULT ('COD'),
        Status           VARCHAR(30) NOT NULL CONSTRAINT DF_Orders_Status DEFAULT ('Pending'),
        Subtotal         DECIMAL(18,2) NOT NULL,
        DeliveryFee      DECIMAL(18,2) NOT NULL CONSTRAINT DF_Orders_DeliveryFee DEFAULT (0),
        TotalAmount      DECIMAL(18,2) NOT NULL,
        CreatedAt        DATETIME2(0) NOT NULL CONSTRAINT DF_Orders_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt        DATETIME2(0) NULL,
        CONSTRAINT UQ_Orders_OrderCode UNIQUE (OrderCode),
        CONSTRAINT CK_Orders_Amounts CHECK
            (Subtotal >= 0 AND DeliveryFee >= 0 AND TotalAmount = Subtotal + DeliveryFee),
        CONSTRAINT CK_Orders_Status CHECK
            (Status IN ('Pending','Confirmed','Preparing','Shipping','Completed','Cancelled')),
        CONSTRAINT CK_Orders_PaymentMethod CHECK
            (PaymentMethod IN ('COD','BankTransfer')),
        CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId)
            REFERENCES dbo.Customers(CustomerId) ON DELETE SET NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.OrderItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems
    (
        OrderItemId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
        OrderId     INT NOT NULL,
        ProductId   INT NULL,
        ProductName NVARCHAR(160) NOT NULL,
        ImageUrl    NVARCHAR(500) NULL,
        UnitPrice   DECIMAL(18,2) NOT NULL,
        Quantity    INT NOT NULL,
        LineTotal   DECIMAL(18,2) NOT NULL,
        CONSTRAINT CK_OrderItems_Values CHECK
            (UnitPrice >= 0 AND Quantity > 0 AND LineTotal = UnitPrice * Quantity),
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId)
            REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
        CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId)
            REFERENCES dbo.Products(ProductId) ON DELETE SET NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.InventoryMovements', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryMovements
    (
        InventoryMovementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryMovements PRIMARY KEY,
        ProductId           INT NOT NULL,
        QuantityChange      INT NOT NULL,
        MovementType        VARCHAR(20) NOT NULL,
        ReferenceCode       NVARCHAR(60) NULL,
        Note                NVARCHAR(500) NULL,
        CreatedAt           DATETIME2(0) NOT NULL CONSTRAINT DF_InventoryMovements_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT CK_InventoryMovements_Quantity CHECK (QuantityChange <> 0),
        CONSTRAINT CK_InventoryMovements_Type CHECK
            (MovementType IN ('Initial','StockIn','Sale','Return','Adjustment','Import')),
        CONSTRAINT FK_InventoryMovements_Products FOREIGN KEY (ProductId)
            REFERENCES dbo.Products(ProductId) ON DELETE NO ACTION
    );
END;
GO

-- ===================================================================
-- 3. CHỈ MỤC TỐI ƯU HIỆU NĂNG TÌM KIẾM
-- ===================================================================
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Category_Active' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_Category_Active ON dbo.Products(CategoryId, IsActive) INCLUDE (Name, Price, StockQuantity);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Name' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_Name ON dbo.Products(Name);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_Status_CreatedAt ON dbo.Orders(Status, CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Orders_CustomerId' AND object_id = OBJECT_ID(N'dbo.Orders'))
    CREATE INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId, CreatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OrderItems_OrderId' AND object_id = OBJECT_ID(N'dbo.OrderItems'))
    CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_InventoryMovements_Product_CreatedAt' AND object_id = OBJECT_ID(N'dbo.InventoryMovements'))
    CREATE INDEX IX_InventoryMovements_Product_CreatedAt ON dbo.InventoryMovements(ProductId, CreatedAt DESC);
GO

-- ===================================================================
-- 4. SEED DỮ LIỆU IDENTITY (ROLES + 5 TÀI KHOẢN MẪU)
-- ===================================================================
BEGIN TRY
    BEGIN TRANSACTION;

    -- 4.1. Khởi tạo các Role: Admin, Nhân viên, Khách hàng, Manager
    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'ADMIN')
        INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES ('role-admin-id', N'Admin', N'ADMIN', NEWID());

    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'NHÂN VIÊN')
        INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES ('role-staff-id', N'Nhân viên', N'NHÂN VIÊN', NEWID());

    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'KHÁCH HÀNG')
        INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES ('role-cust-id', N'Khách hàng', N'KHÁCH HÀNG', NEWID());

    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetRoles WHERE NormalizedName = N'MANAGER')
        INSERT INTO dbo.AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES ('role-mgr-id', N'Manager', N'MANAGER', NEWID());

    -- 4.2. Khởi tạo 5 User chuẩn
    -- Hash cho Admin@123: AQAAAAIAAYagAAAAENEXstaIHWLMEbHZ/xIlV+zhRKA2ht7KubfyxI3o1287tWJWWfLb3jANCDphCYRGew==
    -- Hash cho Staff@123: AQAAAAIAAYagAAAAEIDNzXw6beuUQvQz9AmuFYZplyKdu2Em8ovGu8oX3gd40EFpzEbeCPJsG5IHS8mjDQ==
    -- Hash cho Customer@123: AQAAAAIAAYagAAAAECDedvyB8xyI3UtIeELAfumIUkImozUShxatdSXowamG8Eh0F5rVd3dw82cLn5PQOQ==

    -- 1. Tài khoản Admin: admin@lanu.vn
    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE NormalizedEmail = N'ADMIN@LANU.VN')
    BEGIN
        INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
            PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES ('usr-admin-01', N'admin@lanu.vn', N'ADMIN@LANU.VN', N'admin@lanu.vn', N'ADMIN@LANU.VN', 1,
            N'AQAAAAIAAYagAAAAENEXstaIHWLMEbHZ/xIlV+zhRKA2ht7KubfyxI3o1287tWJWWfLb3jANCDphCYRGew==',
            NEWID(), NEWID(), N'0909999888', 1, 0, 1, 0);

        INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('usr-admin-01', 'role-admin-id');
        INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('usr-admin-01', 'role-mgr-id');
    END;

    -- 2. Tài khoản Nhân viên 1: nhanvien1@lanu.vn
    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE NormalizedEmail = N'NHANVIEN1@LANU.VN')
    BEGIN
        INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
            PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES ('usr-staff-01', N'nhanvien1@lanu.vn', N'NHANVIEN1@LANU.VN', N'nhanvien1@lanu.vn', N'NHANVIEN1@LANU.VN', 1,
            N'AQAAAAIAAYagAAAAEIDNzXw6beuUQvQz9AmuFYZplyKdu2Em8ovGu8oX3gd40EFpzEbeCPJsG5IHS8mjDQ==',
            NEWID(), NEWID(), N'0938111222', 1, 0, 1, 0);

        INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('usr-staff-01', 'role-staff-id');
    END;

    -- 3. Tài khoản Nhân viên 2: nhanvien2@lanu.vn
    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE NormalizedEmail = N'NHANVIEN2@LANU.VN')
    BEGIN
        INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
            PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES ('usr-staff-02', N'nhanvien2@lanu.vn', N'NHANVIEN2@LANU.VN', N'nhanvien2@lanu.vn', N'NHANVIEN2@LANU.VN', 1,
            N'AQAAAAIAAYagAAAAEIDNzXw6beuUQvQz9AmuFYZplyKdu2Em8ovGu8oX3gd40EFpzEbeCPJsG5IHS8mjDQ==',
            NEWID(), NEWID(), N'0938333444', 1, 0, 1, 0);

        INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('usr-staff-02', 'role-staff-id');
    END;

    -- 4. Tài khoản Khách hàng 1: khach1@lanu.vn
    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE NormalizedEmail = N'KHACH1@LANU.VN')
    BEGIN
        INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
            PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES ('usr-cust-01', N'khach1@lanu.vn', N'KHACH1@LANU.VN', N'khach1@lanu.vn', N'KHACH1@LANU.VN', 1,
            N'AQAAAAIAAYagAAAAECDedvyB8xyI3UtIeELAfumIUkImozUShxatdSXowamG8Eh0F5rVd3dw82cLn5PQOQ==',
            NEWID(), NEWID(), N'0901234567', 1, 0, 1, 0);

        INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('usr-cust-01', 'role-cust-id');
    END;

    -- 5. Tài khoản Khách hàng 2: khach2@lanu.vn
    IF NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers WHERE NormalizedEmail = N'KHACH2@LANU.VN')
    BEGIN
        INSERT INTO dbo.AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
            PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
        VALUES ('usr-cust-02', N'khach2@lanu.vn', N'KHACH2@LANU.VN', N'khach2@lanu.vn', N'KHACH2@LANU.VN', 1,
            N'AQAAAAIAAYagAAAAECDedvyB8xyI3UtIeELAfumIUkImozUShxatdSXowamG8Eh0F5rVd3dw82cLn5PQOQ==',
            NEWID(), NEWID(), N'0918765432', 1, 0, 1, 0);

        INSERT INTO dbo.AspNetUserRoles (UserId, RoleId) VALUES ('usr-cust-02', 'role-cust-id');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- ===================================================================
-- 5. SEED DỮ LIỆU CỬA HÀNG (CATEGORIES, PRODUCTS, ORDERS)
-- ===================================================================
BEGIN TRY
    BEGIN TRANSACTION;

    -- 5.1. Seed 5 Danh mục sản phẩm
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = 'bouquet')
        INSERT dbo.Categories (Name, Slug, Description) VALUES (N'Bó hoa tươi', 'bouquet', N'Bó hoa tươi được thiết kế và gói thủ công tỉ mỉ.');
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = 'flower-box')
        INSERT dbo.Categories (Name, Slug, Description) VALUES (N'Hộp hoa cao cấp', 'flower-box', N'Hoa tươi tuyển chọn sắp xếp trang nhã trong hộp quà sang trọng.');
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = 'flower-basket')
        INSERT dbo.Categories (Name, Slug, Description) VALUES (N'Giỏ hoa chúc mừng', 'flower-basket', N'Giỏ hoa sum vầy rực rỡ, thích hợp tặng sinh nhật và sự kiện.');
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = 'seasonal')
        INSERT dbo.Categories (Name, Slug, Description) VALUES (N'Hoa theo mùa', 'seasonal', N'Bộ sưu tập hoa tươi theo mùa nhập khẩu từ các nhà vườn danh tiếng.');
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Slug = 'vase')
        INSERT dbo.Categories (Name, Slug, Description) VALUES (N'Bình hoa nghệ thuật', 'vase', N'Bình hoa cắm sẵn phong cách hiện đại cho không gian sống.');

    -- 5.2. Seed 16 Sản phẩm hoa phong phú với tag nghiệp vụ
    INSERT dbo.Products (CategoryId, Name, Slug, Description, Label, Price, StockQuantity, ImageUrl, IsActive, IsFeatured)
    SELECT c.CategoryId, v.Name, v.Slug, v.Description, v.Label, v.Price, v.StockQuantity, v.ImageUrl, 1, v.IsFeatured
    FROM (VALUES
        (N'bouquet', N'Nắng Đầu Mùa', 'nang-dau-mua', N'Tulip hồng pastel phối baby Hà Lan và lá bạc thanh tao.', N'Bán chạy', CAST(590000 AS DECIMAL(18,2)), 15, N'/assets/tulips.jpg', CAST(1 AS BIT)),
        (N'bouquet', N'Thương Gửi Nàng', 'thuong-gui-nang', N'Hồng Ohara kem phấn, cát tường trắng và thạch thảo tím mộng mơ.', N'Lễ 20/10', CAST(750000 AS DECIMAL(18,2)), 12, N'/assets/pink-bouquet.jpg', CAST(1 AS BIT)),
        (N'flower-box', N'Mộng Mơ Tháng Tư', 'mong-mo-thang-tu', N'Hộp hoa nhung tròn phối hoa hồng Ecuador và cẩm tú cầu lam biếc.', N'Mới', CAST(890000 AS DECIMAL(18,2)), 8, N'/assets/roses.jpg', CAST(1 AS BIT)),
        (N'bouquet', N'Một Ngày Rực Rỡ', 'mot-ngay-ruc-ro', N'Đồng tiền vàng cam, hướng dương mặt trời và cúc tana dịu dàng.', N'Sinh nhật', CAST(490000 AS DECIMAL(18,2)), 20, N'/assets/mixed-flowers.jpg', CAST(1 AS BIT)),
        (N'flower-box', N'Dịu Dàng Sắc Xuân', 'diu-dang-sac-xuan', N'Mẫu đơn hồng kem kết hợp lan hồ điệp mini và lá phụ cao cấp.', N'Giảm giá 15%', CAST(680000 AS DECIMAL(18,2)), 6, N'/assets/peony.jpg', CAST(1 AS BIT)),
        (N'bouquet', N'Lời Yêu Xanh', 'loi-yeu-xanh', N'Tulip kép trắng tuyết, lan vũ nữ vàng và cỏ đồng nội bay bổng.', N'Số lượng có hạn', CAST(820000 AS DECIMAL(18,2)), 4, N'/assets/spring.jpg', CAST(0 AS BIT)),
        (N'seasonal', N'Chớm Nở Yêu Thương', 'chom-no-yeu-thuong', N'Cẩm chướng đỏ tươi phối cúc mẫu đơn và lá chanh Đà Lạt.', N'Hoa theo mùa', CAST(450000 AS DECIMAL(18,2)), 14, N'/assets/seasonal.jpg', CAST(0 AS BIT)),
        (N'flower-box', N'Bình Yên Nơi Phố Thị', 'binh-yen-noi-pho-thi', N'Hoa tone trắng kem tinh khôi, hoa hồng mỡ gà và sao tím.', N'Khai trương', CAST(720000 AS DECIMAL(18,2)), 9, N'/assets/white-bouquet.jpg', CAST(1 AS BIT)),
        (N'flower-basket', N'Hạnh Phúc Đong Đầy', 'hanh-phuc-dong-day', N'Giỏ hoa gỗ vintage cắm tràn hoa hồng Spirit cam và lan mokara.', N'Sinh nhật', CAST(650000 AS DECIMAL(18,2)), 7, N'/assets/mixed-flowers.jpg', CAST(1 AS BIT)),
        (N'vase', N'Bình Hoa Thanh Khiết', 'binh-hoa-thanh-khiet', N'Bình gốm mộc cắm hoa cúc mẫu đơn trắng, tú cầu và phi yến xanh.', N'Mới', CAST(950000 AS DECIMAL(18,2)), 5, N'/assets/white-bouquet.jpg', CAST(0 AS BIT)),
        (N'bouquet', N'Ngọt Ngào Tình Nồng', 'ngot-ngao-tinh-nong', N'99 đóa hồng đỏ Pháp bó giấy đen viền vàng hoàng gia sang trọng.', N'Lễ 20/10', CAST(1450000 AS DECIMAL(18,2)), 3, N'/assets/roses.jpg', CAST(1 AS BIT)),
        (N'flower-basket', N'Khai Trương Hồng Phát', 'khai-truong-hong-phat', N'Giỏ hoa tone đỏ vàng may mắn phối hồng môn và thiên điểu rực rỡ.', N'Khai trương', CAST(1150000 AS DECIMAL(18,2)), 8, N'/assets/spring.jpg', CAST(1 AS BIT)),
        (N'seasonal', N'Thảo Nguyên Xanh Biếc', 'thao-nguyen-xanh-biec', N'Cúc tana đồng nội kết hợp hoa baby trắng tinh khiết như mây trời.', N'Bán chạy', CAST(380000 AS DECIMAL(18,2)), 25, N'/assets/tulips.jpg', CAST(0 AS BIT)),
        (N'bouquet', N'Ánh Dương Ngày Mới', 'anh-duong-ngay-moi', N'Bó hoa hướng dương 5 bông to phối cúc calimero và lá đuôi chồn.', N'Sinh nhật', CAST(420000 AS DECIMAL(18,2)), 18, N'/assets/mixed-flowers.jpg', CAST(0 AS BIT)),
        (N'flower-box', N'Trái Tim Mùa Thu', 'trai-tim-mua-thu', N'Hộp hoa trái tim xếp hoa hồng cam đào cappuccino và socola Ferrero.', N'Giảm giá 20%', CAST(790000 AS DECIMAL(18,2)), 2, N'/assets/pink-bouquet.jpg', CAST(1 AS BIT)),
        (N'vase', N'Lan Hồ Điệp Hoàng Gia', 'lan-ho-diep-hoang-gia', N'Chậu lan hồ điệp 3 cành tím vân sang trọng, tài lộc thịnh vượng.', N'Khai trương', CAST(1200000 AS DECIMAL(18,2)), 5, N'/assets/peony.jpg', CAST(0 AS BIT))
    ) AS v(CategorySlug, Name, Slug, Description, Label, Price, StockQuantity, ImageUrl, IsFeatured)
    INNER JOIN dbo.Categories AS c ON c.Slug = v.CategorySlug
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Products AS p WHERE p.Slug = v.Slug);

    -- 5.3. Khởi tạo lịch sử nhập kho ban đầu
    INSERT dbo.InventoryMovements (ProductId, QuantityChange, MovementType, ReferenceCode, Note)
    SELECT p.ProductId, p.StockQuantity, 'Initial', N'BOOTSTRAP', N'Tồn kho ban đầu của hệ thống demo'
    FROM dbo.Products AS p
    WHERE p.StockQuantity > 0
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.InventoryMovements AS m
          WHERE m.ProductId = p.ProductId
            AND m.MovementType = 'Initial'
            AND m.ReferenceCode = N'BOOTSTRAP'
      );

    -- 5.4. Seed Khách hàng mẫu
    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone = '0901234567')
        INSERT INTO dbo.Customers (FullName, Phone, Email, CreatedAt)
        VALUES (N'Nguyễn Thị Mai', N'0901234567', N'khach1@lanu.vn', SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone = '0918765432')
        INSERT INTO dbo.Customers (FullName, Phone, Email, CreatedAt)
        VALUES (N'Trần Hoàng Nam', N'0918765432', N'khach2@lanu.vn', SYSUTCDATETIME());

    IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Phone = '0933445566')
        INSERT INTO dbo.Customers (FullName, Phone, Email, CreatedAt)
        VALUES (N'Lê Minh Thư', N'0933445566', N'minhthu@gmail.com', SYSUTCDATETIME());

    -- 5.5. Seed 5 Đơn hàng mẫu qua các trạng thái
    DECLARE @CustId1 INT = (SELECT TOP 1 CustomerId FROM dbo.Customers WHERE Phone = '0901234567');
    DECLARE @CustId2 INT = (SELECT TOP 1 CustomerId FROM dbo.Customers WHERE Phone = '0918765432');
    DECLARE @Prod1Id INT = (SELECT TOP 1 ProductId FROM dbo.Products WHERE Slug = 'nang-dau-mua');
    DECLARE @Prod2Id INT = (SELECT TOP 1 ProductId FROM dbo.Products WHERE Slug = 'thuong-gui-nang');
    DECLARE @Prod3Id INT = (SELECT TOP 1 ProductId FROM dbo.Products WHERE Slug = 'mong-mo-thang-tu');
    DECLARE @Prod4Id INT = (SELECT TOP 1 ProductId FROM dbo.Products WHERE Slug = 'mot-ngay-ruc-ro');

    -- Đơn 1: Completed
    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderCode = 'LN20261014001')
    BEGIN
        INSERT INTO dbo.Orders (OrderCode, CustomerId, RecipientName, RecipientPhone, DeliveryAddress, DeliveryDate, CardMessage, CustomerNote, PaymentMethod, Status, Subtotal, DeliveryFee, TotalAmount, CreatedAt)
        VALUES ('LN20261014001', @CustId1, N'Nguyễn Thị Mai', '0901234567',
                N'Số 125 Phan Xích Long, Phường 2, Quận Phú Nhuận, TP. Hồ Chí Minh',
                CAST(DATEADD(day, -2, GETDATE()) AS DATE),
                N'Chúc em yêu ngày 20/10 luôn xinh tươi và rạng rỡ như đóa tulip này nhé!',
                N'[Giao sáng 09:00] Giao tận tay người nhận, gọi trước 10 phút.',
                'BankTransfer', 'Completed', 590000, 0, 590000, DATEADD(day, -2, SYSUTCDATETIME()));

        DECLARE @Oid1 INT = SCOPE_IDENTITY();
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, ImageUrl, UnitPrice, Quantity, LineTotal)
        VALUES (@Oid1, @Prod1Id, N'Nắng Đầu Mùa', N'/assets/tulips.jpg', 590000, 1, 590000);
    END;

    -- Đơn 2: Shipping
    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderCode = 'LN20261015002')
    BEGIN
        INSERT INTO dbo.Orders (OrderCode, CustomerId, RecipientName, RecipientPhone, DeliveryAddress, DeliveryDate, CardMessage, CustomerNote, PaymentMethod, Status, Subtotal, DeliveryFee, TotalAmount, CreatedAt)
        VALUES ('LN20261015002', @CustId2, N'Trần Thu Hà', '0918765432',
                N'Tòa nhà Bitexco, 2 Hải Triều, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
                CAST(GETDATE() AS DATE),
                N'Chúc mừng sinh nhật cô gái tháng 10! Tuổi mới luôn ngập tràn niềm vui và may mắn.',
                N'[Giao chiều 14:00] Gửi lễ tân sảnh G tòa nhà Bitexco.',
                'COD', 'Shipping', 750000, 0, 750000, DATEADD(hour, -4, SYSUTCDATETIME()));

        DECLARE @Oid2 INT = SCOPE_IDENTITY();
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, ImageUrl, UnitPrice, Quantity, LineTotal)
        VALUES (@Oid2, @Prod2Id, N'Thương Gửi Nàng', N'/assets/pink-bouquet.jpg', 750000, 1, 750000);
    END;

    -- Đơn 3: Preparing
    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderCode = 'LN20261016003')
    BEGIN
        INSERT INTO dbo.Orders (OrderCode, CustomerId, RecipientName, RecipientPhone, DeliveryAddress, DeliveryDate, CardMessage, CustomerNote, PaymentMethod, Status, Subtotal, DeliveryFee, TotalAmount, CreatedAt)
        VALUES ('LN20261016003', @CustId1, N'Lê Thị Bích Ngọc', '0933999888',
                N'45 Lê Duẩn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
                CAST(GETDATE() AS DATE),
                N'Mừng khai trương chi nhánh mới hồng phát, tấn tài tấn lộc!',
                N'[Giao chiều 16:30] Cắm hoa kèm banner chúc mừng nền đỏ chữ vàng.',
                'BankTransfer', 'Preparing', 890000, 0, 890000, DATEADD(hour, -2, SYSUTCDATETIME()));

        DECLARE @Oid3 INT = SCOPE_IDENTITY();
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, ImageUrl, UnitPrice, Quantity, LineTotal)
        VALUES (@Oid3, @Prod3Id, N'Mộng Mơ Tháng Tư', N'/assets/roses.jpg', 890000, 1, 890000);
    END;

    -- Đơn 4: Pending (Khách vãng lai / Guest)
    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderCode = 'LN20261016004')
    BEGIN
        INSERT INTO dbo.Orders (OrderCode, CustomerId, RecipientName, RecipientPhone, DeliveryAddress, DeliveryDate, CardMessage, CustomerNote, PaymentMethod, Status, Subtotal, DeliveryFee, TotalAmount, CreatedAt)
        VALUES ('LN20261016004', NULL, N'Hoàng Hải Yến', '0987654321',
                N'88 Nguyễn Thị Minh Khai, Phường Võ Thị Sáu, Quận 3, TP. Hồ Chí Minh',
                CAST(DATEADD(day, 1, GETDATE()) AS DATE),
                N'Chúc em một ngày rạng rỡ yêu đời!',
                N'[Giao sáng 08:30] Khách yêu cầu giao trước 9h sáng.',
                'COD', 'Pending', 490000, 30000, 520000, DATEADD(minute, -30, SYSUTCDATETIME()));

        DECLARE @Oid4 INT = SCOPE_IDENTITY();
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, ImageUrl, UnitPrice, Quantity, LineTotal)
        VALUES (@Oid4, @Prod4Id, N'Một Ngày Rực Rỡ', N'/assets/mixed-flowers.jpg', 490000, 1, 490000);
    END;

    -- Đơn 5: Cancelled
    IF NOT EXISTS (SELECT 1 FROM dbo.Orders WHERE OrderCode = 'LN20261013005')
    BEGIN
        INSERT INTO dbo.Orders (OrderCode, CustomerId, RecipientName, RecipientPhone, DeliveryAddress, DeliveryDate, CardMessage, CustomerNote, PaymentMethod, Status, Subtotal, DeliveryFee, TotalAmount, CreatedAt)
        VALUES ('LN20261013005', @CustId2, N'Đặng Văn Kiên', '0918765432',
                N'12 Hoàng Hoa Thám, Phường 12, Quận Tân Bình, TP. Hồ Chí Minh',
                CAST(DATEADD(day, -3, GETDATE()) AS DATE),
                NULL,
                N'Khách báo đổi lịch công tác nên xin hủy đơn.',
                'COD', 'Cancelled', 590000, 0, 590000, DATEADD(day, -3, SYSUTCDATETIME()));

        DECLARE @Oid5 INT = SCOPE_IDENTITY();
        INSERT INTO dbo.OrderItems (OrderId, ProductId, ProductName, ImageUrl, UnitPrice, Quantity, LineTotal)
        VALUES (@Oid5, @Prod1Id, N'Nắng Đầu Mùa', N'/assets/tulips.jpg', 590000, 1, 590000);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- ===================================================================
-- KIỂM TRA KẾT QUẢ KHỞI TẠO DỮ LIỆU
-- ===================================================================
SELECT 'Danh mục hoa' AS [Bảng], COUNT(*) AS [Tổng số] FROM dbo.Categories
UNION ALL
SELECT 'Sản phẩm hoa', COUNT(*) FROM dbo.Products
UNION ALL
SELECT 'Đơn hàng mẫu', COUNT(*) FROM dbo.Orders
UNION ALL
SELECT 'Vai trò (Roles)', COUNT(*) FROM dbo.AspNetRoles
UNION ALL
SELECT 'Tài khoản mẫu (Users)', COUNT(*) FROM dbo.AspNetUsers;
GO
