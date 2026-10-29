using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.QuanLy;

public class DashboardModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    public string TenDangNhap { get; set; }
    public string TenHienThi { get; set; }
    public ChiNhanhShowroom Showroom { get; set; }
    public TaiKhoan? TaiKhoan { get; set; }
    public long DoanhThuHomNay { get; set; }
    public int SoDonHomNay { get; set; }
    public long DoanhThuThangNay { get; set; }
    public int SoDonThangNay { get; set; }
    public int TongDon { get; set; }
    public long DoanhThuCocThangNay { get; set; }

    [BindProperty]
    public string NewTenHienThi { get; set; }
    [BindProperty]
    public string? NewEmail { get; set; }
    [BindProperty]
    public string? NewSoDienThoai { get; set; }
    [BindProperty]
    public string? NewDiaChi { get; set; }

    [BindProperty]
    public string? NewShowroomDiaChi { get; set; }
    [BindProperty]
    public string? NewShowroomThanhPho { get; set; }
    [BindProperty]
    public string? NewShowroomDuongDayNong { get; set; }

    public string? Message { get; set; }
    public string? MessageType { get; set; }

    public List<DailyRevenue> DoanhThuTheoNgay { get; set; } = new();
    public string JsonLabels { get; set; }
    public string JsonData { get; set; }

    public DashboardModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userName = User.GetJwtUserName();
        var role = User.GetJwtRole();

        if (string.IsNullOrEmpty(userName))
            return RedirectToPage("/Account/Login");

        TenDangNhap = userName;
        TenHienThi = User.GetJwtDisplayName() ?? userName;

        TaiKhoan = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == userName);

        // Tìm showroom được giao quản lý
        Showroom = await _db.ChiNhanhShowroom
            .FirstOrDefaultAsync(c => c.MaQuanLy == userName);

        if (Showroom == null)
            return Page();

        var homNay = DateTime.Today;
        var dauThang = new DateTime(homNay.Year, homNay.Month, 1);
        var truoc30Ngay = homNay.AddDays(-30);

        // Doanh thu = doanh thu ban xe thuc te tu hoa don (HoaDonMuaXe) thuoc showroom nay
        var hdQuery = _db.HoaDonMuaXe
            .Where(h => h.MaChiNhanh == Showroom.MaChiNhanh);

        DoanhThuHomNay = await hdQuery
            .Where(h => h.NgayXuatHoaDon >= homNay)
            .SumAsync(h => (long?)h.TongTienPhaiTra) ?? 0;
        SoDonHomNay = await hdQuery
            .Where(h => h.NgayXuatHoaDon >= homNay)
            .CountAsync();

        DoanhThuThangNay = await hdQuery
            .Where(h => h.NgayXuatHoaDon >= dauThang)
            .SumAsync(h => (long?)h.TongTienPhaiTra) ?? 0;
        SoDonThangNay = await hdQuery
            .Where(h => h.NgayXuatHoaDon >= dauThang)
            .CountAsync();

        // So hoa don da xuat cua showroom tu truoc den nay
        TongDon = await hdQuery.CountAsync();

        // Tien coc thu ve thang nay (phan bo theo showroom -> ThongKeTongHop_Boss)
        var kyBaoCao = DateTime.Now.ToString("yyyy-MM");
        var thongKe = await _db.ThongKeTongHop_Boss
            .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == Showroom.MaChiNhanh);
        DoanhThuCocThangNay = thongKe?.TongTienCocThuVe ?? 0;

        // Doanh thu 30 ngay gan nhat (cho bieu do) — theo ngay xuat hoa don
        var dailyData = await hdQuery
            .Where(h => h.NgayXuatHoaDon.Date >= truoc30Ngay)
            .GroupBy(h => h.NgayXuatHoaDon.Date)
            .Select(g => new { Ngay = g.Key, Tong = g.Sum(h => (long?)h.TongTienPhaiTra) ?? 0 })
            .OrderBy(g => g.Ngay)
            .ToListAsync();

        // Fill missing dates
        var labels = new List<string>();
        var values = new List<long>();
        var dataDict = dailyData.ToDictionary(d => d.Ngay, d => d.Tong);

        for (var date = truoc30Ngay.Date; date <= homNay; date = date.AddDays(1))
        {
            labels.Add(date.ToString("dd/MM"));
            values.Add(dataDict.GetValueOrDefault(date, 0));
        }

        JsonLabels = System.Text.Json.JsonSerializer.Serialize(labels);
        JsonData = System.Text.Json.JsonSerializer.Serialize(values);

        DoanhThuTheoNgay = dailyData.Select(d => new DailyRevenue
        {
            Ngay = d.Ngay,
            TongTien = d.Tong
        }).ToList();

        await _log.LogAsync("Quản Lý xem Dashboard", $"showroom {Showroom.TenChiNhanh}");
        return Page();
    }

    public async Task<IActionResult> OnPostProfileAsync()
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var user = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == userName);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(NewTenHienThi)) user.TenHienThi = NewTenHienThi.Trim();
        if (!string.IsNullOrWhiteSpace(NewEmail)) user.Email = NewEmail.Trim();
        if (!string.IsNullOrWhiteSpace(NewSoDienThoai)) user.SoDienThoai = NewSoDienThoai.Trim();
        if (!string.IsNullOrWhiteSpace(NewDiaChi)) user.DiaChi = NewDiaChi.Trim();

        await _db.SaveChangesAsync();

        var jwt = HttpContext.RequestServices.GetRequiredService<IJwtService>();
        HttpContext.SetJwtCookie(jwt.GenerateToken(user));

        Message = "Cập nhật thông tin cá nhân thành công.";
        MessageType = "success";
        await _log.LogAsync("Quản Lý cập nhật thông tin cá nhân", userName);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostShowroomAsync()
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var showroom = await _db.ChiNhanhShowroom
            .FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(NewShowroomDiaChi)) showroom.DiaChi = NewShowroomDiaChi.Trim();
        if (!string.IsNullOrWhiteSpace(NewShowroomThanhPho)) showroom.ThanhPho = NewShowroomThanhPho.Trim();
        if (!string.IsNullOrWhiteSpace(NewShowroomDuongDayNong)) showroom.DuongDayNong = NewShowroomDuongDayNong.Trim();

        await _db.SaveChangesAsync();

        Message = "Cập nhật thông tin showroom thành công.";
        MessageType = "success";
        await _log.LogAsync("Quản Lý cập nhật thông tin showroom", userName);
        return RedirectToPage();
    }
}

public class DailyRevenue
{
    public DateTime Ngay { get; set; }
    public long TongTien { get; set; }
}
