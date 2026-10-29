using CarProject.Data;
using CarProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarProject.Services;

public interface INotificationService
{
    Task SendAsync(string maNguoiNhan, string tieuDe, string noiDung, string? duongDan = null);
    Task SendToRoleAsync(string vaiTro, string tieuDe, string noiDung, string? duongDan = null);
    Task<List<ThongBao>> GetRecentAsync(string maNguoiNhan, int count = 10);
    Task<int> GetUnreadCountAsync(string maNguoiNhan);
    Task MarkAsReadAsync(int maThongBao);
    Task MarkAllAsReadAsync(string maNguoiNhan);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SendAsync(string maNguoiNhan, string tieuDe, string noiDung, string? duongDan = null)
    {
        var notif = new ThongBao
        {
            MaNguoiNhan = maNguoiNhan,
            TieuDe = tieuDe,
            NoiDung = noiDung,
            DuongDan = duongDan ?? "",
            DaXem = false,
            NgayTao = DateTime.Now
        };
        _db.ThongBao.Add(notif);
        await _db.SaveChangesAsync();
    }

    public async Task SendToRoleAsync(string vaiTro, string tieuDe, string noiDung, string? duongDan = null)
    {
        var users = await _db.TaiKhoan
            .Where(t => t.VaiTro == vaiTro && (t.TrangThai == "Active" || t.TrangThai == "Hoạt động"))
            .ToListAsync();

        foreach (var user in users)
        {
            await SendAsync(user.TenDangNhap, tieuDe, noiDung, duongDan);
        }
    }

    public async Task<List<ThongBao>> GetRecentAsync(string maNguoiNhan, int count = 10)
    {
        return await _db.ThongBao
            .Where(t => t.MaNguoiNhan == maNguoiNhan)
            .OrderByDescending(t => t.NgayTao)
            .Take(count)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(string maNguoiNhan)
    {
        return await _db.ThongBao
            .CountAsync(t => t.MaNguoiNhan == maNguoiNhan && !t.DaXem);
    }

    public async Task MarkAsReadAsync(int maThongBao)
    {
        var notif = await _db.ThongBao.FindAsync(maThongBao);
        if (notif != null)
        {
            notif.DaXem = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string maNguoiNhan)
    {
        var unread = await _db.ThongBao
            .Where(t => t.MaNguoiNhan == maNguoiNhan && !t.DaXem)
            .ToListAsync();

        foreach (var n in unread)
            n.DaXem = true;

    await _db.SaveChangesAsync();
    }
}
