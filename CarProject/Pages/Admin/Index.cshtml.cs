using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Services;

namespace CarProject.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    public int TotalHangXe { get; set; }
    public int TotalDongXe { get; set; }
    public int TotalPhienBan { get; set; }
    public int TotalLogs { get; set; }
    public int TotalUsers { get; set; }
    public int TotalChiNhanh { get; set; }
    public int TotalKhuyenMai { get; set; }
    public int TotalDonCoc { get; set; }
    public int TotalHoaDon { get; set; }
    public int TotalLichHen { get; set; }
    public int TotalKenhTuVan { get; set; }
    public int TotalThongKe { get; set; }

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        // Check login
        if (!User.IsJwtLoggedIn())
        {
            RedirectToPage("/Account/Login");
            return;
        }

        TotalHangXe = await _db.HangXe.CountAsync();
        TotalDongXe = await _db.DongXe.CountAsync();
        TotalPhienBan = await _db.PhienBanXe.CountAsync();
        TotalLogs = await _db.NhatKyHeThong.CountAsync();
        TotalUsers = await _db.TaiKhoan.CountAsync();
        TotalChiNhanh = await _db.ChiNhanhShowroom.CountAsync();
        TotalKhuyenMai = await _db.ChuongTrinhKhuyenMai.CountAsync();
        TotalDonCoc = await _db.DonDatCoc.CountAsync();
        TotalHoaDon = await _db.HoaDonMuaXe.CountAsync();
        TotalLichHen = await _db.LichHenLaiThu.CountAsync();
        TotalKenhTuVan = await _db.KenhTuVan.CountAsync();
        TotalThongKe = await _db.ThongKeTongHop_Boss.CountAsync();

        await _log.LogAsync("Xem Admin Dashboard");
    }
}
