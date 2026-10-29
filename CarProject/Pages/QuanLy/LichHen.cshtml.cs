using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.QuanLy;

public class LichHenModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly INotificationService _notif;

    public LichHenModel(AppDbContext db, IActivityLogService log, INotificationService notif)
    {
        _db = db;
        _log = log;
        _notif = notif;
    }

    public ChiNhanhShowroom? Showroom { get; set; }
    public List<LichHenLaiThu> DanhSachLichHen { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public IEnumerable<LichHenLaiThu> LichHenLaiThu => DanhSachLichHen.Where(l => IsTestDrive(l));
    public IEnumerable<LichHenLaiThu> LichHenMuaXe => DanhSachLichHen.Where(l => IsPurchase(l));

    public bool IsTestDrive(LichHenLaiThu lichHen)
        => lichHen.TrangThai.Contains("Lái thử") || !lichHen.TrangThai.Contains("Mua xe");

    public bool IsPurchase(LichHenLaiThu lichHen)
        => lichHen.TrangThai.Contains("Mua xe");

    public string GetBaseStatus(string trangThai)
    {
        if (trangThai.StartsWith("Chờ xác nhận")) return "Chờ xác nhận";
        if (trangThai.StartsWith("Đã xác nhận")) return "Đã xác nhận";
        if (trangThai.StartsWith("Từ chối")) return "Từ chối";
        if (trangThai.StartsWith("Hoàn thành - Bán")) return "Hoàn thành - Bán";
        if (trangThai.StartsWith("Hoàn thành - Không bán")) return "Hoàn thành - Không bán";
        return trangThai;
    }

    public string GetTypeLabel(LichHenLaiThu lichHen)
        => IsPurchase(lichHen) ? "Mua xe" : "Lái thử";

    private string JoinTypeSuffix(string trangThai)
    {
        if (trangThai.Contains("Mua xe")) return " - Mua xe";
        return " - Lái thử";
    }

    private string WithTypeSuffix(string baseStatus, string trangThai)
        => baseStatus + JoinTypeSuffix(trangThai);

    public async Task<IActionResult> OnGetAsync()
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName))
            return RedirectToPage("/Account/Login");

        Showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (Showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return Page();
        }

        DanhSachLichHen = await _db.LichHenLaiThu
            .Include(l => l.DongXe)
            .Include(l => l.KhachHang)
            .Where(l => l.MaChiNhanh == Showroom.MaChiNhanh)
            .OrderByDescending(l => l.NgayHen)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAcceptAsync(int maLichHen)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        Showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (Showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return RedirectToPage();
        }

        var lichHen = await _db.LichHenLaiThu.FindAsync(maLichHen);
        if (lichHen == null) return NotFound();
        if (lichHen.MaChiNhanh != Showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền duyệt lịch hẹn này.";
            return RedirectToPage();
        }

        lichHen.TrangThai = WithTypeSuffix("Đã xác nhận", lichHen.TrangThai);
        await _db.SaveChangesAsync();

        await _notif.SendAsync(lichHen.MaKhachHang, "Lịch hẹn đã được xác nhận",
            $"Lịch hẹn xe vào {lichHen.NgayHen:dd/MM/yyyy} lúc {lichHen.GioHen} đã được xác nhận.", "/TestDrive");
        await _log.LogAsync($"QL duyệt lịch hẹn #{maLichHen}");

        SuccessMessage = "Đã xác nhận lịch hẹn.";
        return RedirectToPage();
    }

    private async Task CapNhatDoanhThuLaiThuAsync(LichHenLaiThu lichHen, bool daBan)
    {
        var kyBaoCao = DateTime.Now.ToString("yyyy-MM");

        var saleAmount = 0L;
        if (daBan)
        {
            var phienBan = await _db.PhienBanXe
                .Where(p => p.MaDong == lichHen.MaDong)
                .OrderByDescending(p => p.SoLuongTrongKho)
                .ThenBy(p => p.MaPhienBan)
                .FirstOrDefaultAsync();
            if (phienBan != null)
            {
                saleAmount = phienBan.GiaNiemYet;
                phienBan.SoLuongTrongKho = Math.Max(0, phienBan.SoLuongTrongKho - 1);
            }
        }

        var thongKe = await _db.ThongKeTongHop_Boss
            .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == lichHen.MaChiNhanh);

        if (thongKe == null)
        {
            thongKe = new ThongKeTongHop_Boss
            {
                KyBaoCao = kyBaoCao,
                MaChiNhanh = lichHen.MaChiNhanh,
                TongDoanhThu = saleAmount,
                TongTienCocThuVe = 0,
                TongSoXeDaBan = daBan ? 1 : 0,
                SoDonCocBiHuy = 0,
                TongLuotXemWeb = 0,
                TongLuotLaiThu = 1,
                MaDongXeBanChayNhat = daBan ? lichHen.MaDong : 0
            };
            _db.ThongKeTongHop_Boss.Add(thongKe);
        }
        else
        {
            thongKe.TongLuotLaiThu += 1;
            if (daBan)
            {
                thongKe.TongSoXeDaBan += 1;
                thongKe.TongDoanhThu += saleAmount;
                if (thongKe.MaDongXeBanChayNhat == 0)
                {
                    thongKe.MaDongXeBanChayNhat = lichHen.MaDong;
                }
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IActionResult> OnPostRejectAsync(int maLichHen)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        Showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (Showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return RedirectToPage();
        }

        var lichHen = await _db.LichHenLaiThu.FindAsync(maLichHen);
        if (lichHen == null) return NotFound();
        if (lichHen.MaChiNhanh != Showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền xử lý lịch hẹn này.";
            return RedirectToPage();
        }

        lichHen.TrangThai = WithTypeSuffix("Từ chối", lichHen.TrangThai);
        await _db.SaveChangesAsync();

        await _notif.SendAsync(lichHen.MaKhachHang, "Lịch hẹn bị từ chối",
            $"Lịch hẹn xe vào {lichHen.NgayHen:dd/MM/yyyy} lúc {lichHen.GioHen} đã bị từ chối. Vui lòng liên hệ showroom để biết thêm chi tiết.", "/TestDrive");
        await _log.LogAsync($"QL từ chối lịch hẹn #{maLichHen}");

        SuccessMessage = "Đã từ chối lịch hẹn.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCompleteAsync(int maLichHen, bool daBan = false)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        Showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (Showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return RedirectToPage();
        }

        var lichHen = await _db.LichHenLaiThu.FindAsync(maLichHen);
        if (lichHen == null) return NotFound();
        if (lichHen.MaChiNhanh != Showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền xử lý lịch hẹn này.";
            return RedirectToPage();
        }

        if (!lichHen.TrangThai.StartsWith("Đã xác nhận"))
        {
            ErrorMessage = "Chỉ có thể hoàn thành lịch hẹn đã được xác nhận.";
            return RedirectToPage();
        }

        var status = daBan ? "Hoàn thành - Bán" : "Hoàn thành - Không bán";
        lichHen.TrangThai = WithTypeSuffix(status, lichHen.TrangThai);
        await CapNhatDoanhThuLaiThuAsync(lichHen, daBan);
        await _db.SaveChangesAsync();

        await _log.LogAsync($"QL hoàn thành lịch hẹn #{maLichHen} - {(daBan ? "Bán" : "Không bán")}");

        SuccessMessage = daBan ? "Đã hoàn thành lịch hẹn và đánh dấu bán." : "Đã hoàn thành lịch hẹn (không bán).";
        return RedirectToPage();
    }
}