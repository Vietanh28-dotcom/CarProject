using CarProject.Data;
using CarProject.Models;
using Serilog;

namespace CarProject.Services;

public interface IActivityLogService
{
    Task LogAsync(string hanhDong, string chiTiet = null);
}

public class ActivityLogService : IActivityLogService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public ActivityLogService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task LogAsync(string hanhDong, string chiTiet = null)
    {
        var ctx = _http.HttpContext;
        var userName = ctx?.User.GetJwtUserName() ?? "(anonymous)";
        var role = ctx?.User.GetJwtRole() ?? "Guest";
        var ip = ctx?.Connection.RemoteIpAddress?.ToString();
        var path = ctx?.Request.Path;

        // Ghi ra file log Serilog (không cần DB)
        Log.Information("[{Role}] {User} | {Action} | {Detail} | {Path} | {IP}",
            role, userName, hanhDong, chiTiet, path, ip);

        // Ghi vào DB nếu có quyền (bỏ qua lỗi nếu không)
        try
        {
            var entry = new NhatKyHeThong
            {
                MaTaiKhoan = ctx?.User.GetJwtUserName() ?? "(anonymous)",
                TenDangNhap = userName,
                VaiTro = role,
                HanhDong = hanhDong,
                ChiTiet = chiTiet ?? "",
                DiaChiIP = ip,
                TrinhDuyet = ctx?.Request.Headers.UserAgent.ToString(),
                DuongDan = path,
                ThoiGian = DateTime.UtcNow
            };
            _db.NhatKyHeThong.Add(entry);
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Gỡ entity lỗi khỏi change tracker để không ảnh hưởng các lần SaveChanges sau
            var broken = _db.ChangeTracker.Entries<NhatKyHeThong>()
                .Where(e => e.State == Microsoft.EntityFrameworkCore.EntityState.Added)
                .ToList();
            foreach (var b in broken)
                b.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
        }
    }
}
