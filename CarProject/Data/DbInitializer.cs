using CarProject.Models;
using CarProject.Services;
using Microsoft.EntityFrameworkCore;

namespace CarProject.Data;

public static class DbInitializer
{
    public static void SeedData(AppDbContext context)
    {
        // Tạo SQL Login AppReader (chỉ đọc, KHÔNG xem được bảng TaiKhoan)
        try
        {
            context.Database.ExecuteSqlRaw(@"
                IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'AppReader')
                BEGIN
                    CREATE LOGIN AppReader WITH PASSWORD = 'Abc@123456';
                    CREATE USER AppReader FOR LOGIN AppReader;
                    ALTER ROLE db_datareader ADD MEMBER AppReader;
                END
                DENY SELECT ON TaiKhoan TO AppReader;
                DENY SELECT ON NhatKyHeThong TO AppReader;
            ");
        }
        catch { /* login may already exist or not supported */ }

        // Seed test users
        if (!context.TaiKhoan.Any())
        {
            var pwd = new PasswordService();
            context.TaiKhoan.AddRange(
                new TaiKhoan
                {
                    TenDangNhap = "admin",
                    MatKhau = pwd.Hash("admin123"),
                    VaiTro = "Admin",
                    TrangThai = "Active",
                    TenHienThi = "Admin",
                    AvatarUrl = "/uploads/avatars/default.png",
                    Email = "admin@carproject.com",
                    DaXacNhanEmail = true
                },
                new TaiKhoan
                {
                    TenDangNhap = "user",
                    MatKhau = pwd.Hash("user123"),
                    VaiTro = "User",
                    TrangThai = "Active",
                    TenHienThi = "Người dùng",
                    AvatarUrl = "/uploads/avatars/default.png",
                    Email = "user@carproject.com",
                    DaXacNhanEmail = true
                }
            );
            context.SaveChanges();
        }
    }
}
