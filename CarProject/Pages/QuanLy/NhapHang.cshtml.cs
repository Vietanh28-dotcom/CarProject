using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.QuanLy;

public class NhapHangModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly INotificationService _notif;

    public ChiNhanhShowroom? Showroom { get; set; }
    public List<PhieuNhapXe> PhieuNhaps { get; set; } = new();
    public List<PhienBanXe> PhienBans { get; set; } = new();
    public Dictionary<int, int> TonKhoShowroom { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public NhapHangModel(AppDbContext db, IActivityLogService log, INotificationService notif)
    {
        _db = db;
        _log = log;
        _notif = notif;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        Showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (Showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return Page();
        }

        DanhSachPhienBan(Showroom.MaChiNhanh);
        return Page();
    }

    public async Task<IActionResult> OnPostNhapAsync(int maPhienBan, int soLuongNhap, string? ghiChu)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        Showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (Showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return RedirectToPage();
        }

        if (soLuongNhap <= 0)
        {
            ErrorMessage = "Số lượng nhập phải lớn hơn 0.";
            return RedirectToPage();
        }

        var phienBan = await _db.PhienBanXe
            .Include(p => p.DongXe)
            .FirstOrDefaultAsync(p => p.MaPhienBan == maPhienBan);
        if (phienBan == null)
        {
            ErrorMessage = "Không tìm thấy phiên bản xe.";
            return RedirectToPage();
        }

        // Giá nhập lấy từ cột GiaNhap của phiên bản (admin/chạy script đã set)
        long giaNhap = phienBan.GiaNhap > 0 ? phienBan.GiaNhap : phienBan.GiaNiemYet;
        phienBan.GiaNhap = giaNhap;
        var tongSoTienNhap = giaNhap * soLuongNhap;

        // 1. Tăng tồn kho showroom
        var tonKho = await _db.TonKhoTheoChiNhanh
            .FirstOrDefaultAsync(t => t.MaPhienBan == maPhienBan && t.MaChiNhanh == Showroom.MaChiNhanh);
        if (tonKho == null)
        {
            _db.TonKhoTheoChiNhanh.Add(new TonKhoTheoChiNhanh
            {
                MaPhienBan = maPhienBan,
                MaChiNhanh = Showroom.MaChiNhanh,
                SoLuong = soLuongNhap,
                NgayCapNhat = DateTime.Now
            });
        }
        else
        {
            tonKho.SoLuong += soLuongNhap;
            tonKho.NgayCapNhat = DateTime.Now;
        }

        // 2. Tăng tồn kho tổng phiên bản
        phienBan.SoLuongTrongKho += soLuongNhap;

        // 3. Ghi phiếu nhập
        _db.PhieuNhapXe.Add(new PhieuNhapXe
        {
            MaDonCoc = null,
            MaPhienBan = maPhienBan,
            MaChiNhanh = Showroom.MaChiNhanh,
            SoLuongNhap = soLuongNhap,
            SoTienNhapMoiXe = giaNhap,
            TongSoTienNhap = tongSoTienNhap,
            NguoiNhap = userName,
            NgayNhap = DateTime.Now,
            GhiChu = ghiChu
        });

        // 4. Trừ doanh thu showroom = tổng tiền nhập
        await GiamDoanhThuNhapHangAsync(Showroom.MaChiNhanh, tongSoTienNhap);

        await _db.SaveChangesAsync();

        var tenXe = $"{phienBan.DongXe?.TenDong ?? ""} {phienBan.TenPhienBan}".Trim();
        await _notif.SendToRoleAsync("Admin", "Quản Lý nhập hàng",
            $"{Showroom.TenChiNhanh} vừa nhập {soLuongNhap} xe {tenXe} (giá {giaNhap:N0}đ/xe, tổng {tongSoTienNhap:N0}đ).", "/QuanLy/NhapHang");
        await _log.LogAsync("QL nhập kho", $"nhập {soLuongNhap} xe {tenXe} vào {Showroom.MaChiNhanh}, giá {giaNhap:N0}đ/xe");

        SuccessMessage = $"Đã nhập {soLuongNhap} xe {tenXe} vào kho với tổng {tongSoTienNhap:N0}đ (đã trừ doanh thu).";
        return RedirectToPage();
    }

    private void DanhSachPhienBan(string maChiNhanh)
    {
        PhienBans = _db.PhienBanXe
            .Include(p => p.DongXe)
            .OrderBy(p => p.DongXe.TenDong).ThenBy(p => p.TenPhienBan)
            .ToList();
        TonKhoShowroom = _db.TonKhoTheoChiNhanh
            .Where(t => t.MaChiNhanh == maChiNhanh)
            .GroupBy(t => t.MaPhienBan)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SoLuong));
        PhieuNhaps = _db.PhieuNhapXe
            .Include(p => p.PhienBan).ThenInclude(p => p.DongXe)
            .Where(p => p.MaChiNhanh == maChiNhanh)
            .OrderByDescending(p => p.NgayNhap)
            .Take(20)
            .ToList();
    }

    private async Task GiamDoanhThuNhapHangAsync(string maChiNhanh, long soTienNhap)
    {
        if (string.IsNullOrEmpty(maChiNhanh) || soTienNhap <= 0) return;

        var kyBaoCao = DateTime.Now.ToString("yyyy-MM");
        var thongKe = await _db.ThongKeTongHop_Boss
            .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == maChiNhanh);

        if (thongKe == null)
        {
            _db.ThongKeTongHop_Boss.Add(new ThongKeTongHop_Boss
            {
                KyBaoCao = kyBaoCao,
                MaChiNhanh = maChiNhanh,
                TongDoanhThu = -soTienNhap,
                TongTienCocThuVe = 0,
                TongSoXeDaBan = 0,
                SoDonCocBiHuy = 0,
                TongLuotXemWeb = 0,
                TongLuotLaiThu = 0,
                MaDongXeBanChayNhat = 1
            });
        }
        else
        {
            thongKe.TongDoanhThu -= soTienNhap;
            if (thongKe.MaDongXeBanChayNhat == 0) thongKe.MaDongXeBanChayNhat = 1;
        }
    }
}