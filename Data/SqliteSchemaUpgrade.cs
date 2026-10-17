using Microsoft.EntityFrameworkCore;

namespace LTW09.Data;

// Bổ sung bảng mới cho cả file SQLite đã chạy ở bản trước; không xóa/khởi tạo lại dữ liệu.
public static class SqliteSchemaUpgrade
{
    public static async Task ApplyAsync(FlowerShopDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ProductReviews" (
                "ProductReviewId" INTEGER NOT NULL CONSTRAINT "PK_ProductReviews" PRIMARY KEY AUTOINCREMENT,
                "ProductId" INTEGER NOT NULL,
                "UserId" TEXT NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "Stars" INTEGER NOT NULL CONSTRAINT "CK_ProductReviews_Stars" CHECK ("Stars" BETWEEN 1 AND 5),
                "Comment" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_ProductReviews_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("ProductId") ON DELETE CASCADE,
                CONSTRAINT "FK_ProductReviews_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_ProductReviews_ProductId_UserId" ON "ProductReviews" ("ProductId", "UserId");
            CREATE INDEX IF NOT EXISTS "IX_ProductReviews_ProductId_Stars" ON "ProductReviews" ("ProductId", "Stars");
            CREATE INDEX IF NOT EXISTS "IX_ProductReviews_UserId" ON "ProductReviews" ("UserId");
            """);
    }
}
