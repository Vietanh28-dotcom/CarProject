using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.QuanLy;

public class KhoItem
{
    public int MaPhienBan { get; set; }
    public string TenXe { get; set; } = "";
    public long GiaNiemYet { get; set; }
    public long GiaNhap { get; set; }
    public int TonKhoCuaToi { get; set; }
    public int TonTong { get; set; }
    public bool CoDonCoc { get; set; }
}

public class KhoModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly INotificationService _notif;

    public ChiNhanhShowroom? Showroom { get; set; }
    public List<KhoItem> KhoItems { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public KhoModel(AppDbContext db, IActivityLogService log, INotificationService notif)
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

        await LoadKhoAsync(Showroom.MaChiNhanh);
        return Page();
    }

    private async Task LoadKhoAsync(string maChiNhanh)
    {
        var phienBans = await _db.PhienBanXe
            .Include(p => p.DongXe)
            .OrderBy(p => p.DongXe.TenDong).ThenBy(p => p.TenPhienBan)
            .ToListAsync();

        var tonKhoLut = (await _db.TonKhoTheoChiNhanh
                .Where(t => t.MaChiNhanh == maChiNhanh)
                .ToListAsync())
            .GroupBy(t => t.MaPhienBan)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SoLuong));

        var pbIds = phienBans.Select(p => p.MaPhienBan).ToList();
        var coDonCocSet = (await _db.DonDatCocChiTiet
                .Where(c => pbIds.Contains(c.MaPhienBan)
                    && c.DonDatCoc != null
                    && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã từ chối"
                    && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất")
                .Select(c => c.MaPhienBan)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        KhoItems = phienBans.Select(p => new KhoItem
        {
            MaPhienBan = p.MaPhienBan,
            TenXe = $"{p.DongXe?.TenDong ?? ""} {p.TenPhienBan}".Trim(),
            GiaNiemYet = p.GiaNiemYet,
            GiaNhap = p.GiaNhap,
            TonKhoCuaToi = tonKhoLut.TryGetValue(p.MaPhienBan, out var sl) ? sl : 0,
            TonTong = p.SoLuongTrongKho,
            CoDonCoc = coDonCocSet.Contains(p.MaPhienBan)
        }).ToList();
    }

    public async Task<IActionResult> OnPostBanAsync(int maPhienBan, int soLuong)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        Showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (Showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return RedirectToPage();
        }

        if (soLuong <= 0)
        {
            ErrorMessage = "Số lượng bán phải lớn hơn 0.";
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

        var tonKho = await _db.TonKhoTheoChiNhanh
            .FirstOrDefaultAsync(t => t.MaPhienBan == maPhienBan && t.MaChiNhanh == Showroom.MaChiNhanh);
        var tonKhoCuaToi = tonKho?.SoLuong ?? 0;
        if (tonKho == null || tonKhoCuaToi < soLuong)
        {
            ErrorMessage = $"Kho của bạn chỉ còn {tonKhoCuaToi} xe, không đủ để bán {soLuong}.";
            return RedirectToPage();
        }

        // Điều kiện: xe không được dính đơn cọc nào (đơn đã thanh toán còn giữ xe)
        var coDonCoc = await _db.DonDatCocChiTiet
            .AnyAsync(c => c.MaPhienBan == maPhienBan
                && c.DonDatCoc != null
                && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã từ chối"
                && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất");
        if (coDonCoc)
        {
            ErrorMessage = "Xe này đang dính đơn đặt cọc (đã thanh toán giữ xe), không được bán trực tiếp.";
            return RedirectToPage();
        }

        // Trừ tồn kho vật lý của showroom + tồn kho tổng phiên bản
        tonKho.SoLuong = Math.Max(0, tonKho.SoLuong - soLuong);
        tonKho.NgayCapNhat = DateTime.Now;
        phienBan.SoLuongTrongKho = Math.Max(0, phienBan.SoLuongTrongKho - soLuong);

        // Tăng doanh thu = chênh lệch lãi (giá bán - giá nhập)
        long laiMotXe = Math.Max(0, phienBan.GiaNiemYet - phienBan.GiaNhap);
        long tongLai = laiMotXe * soLuong;
        await TangDoanhThuBanThangAsync(Showroom.MaChiNhanh, tongLai, soLuong, phienBan.MaDong);

        await _db.SaveChangesAsync();

        var tenXe = $"{phienBan.DongXe?.TenDong ?? ""} {phienBan.TenPhienBan}".Trim();
        await _notif.SendToRoleAsync("Admin", "Quản Lý bán trực tiếp từ kho",
            $"{Showroom.TenChiNhanh} vừa bán trực tiếp {soLuong} xe {tenXe} (lãi {tongLai:N0}đ).", "/QuanLy/Kho");
        await _log.LogAsync("QL bán trực tiếp",
            $"bán {soLuong} xe {tenXe} từ kho {Showroom.MaChiNhanh}, lãi {tongLai:N0}đ");

        SuccessMessage = $"Đã bán {soLuong} xe {tenXe} từ kho. Doanh thu tăng {tongLai:N0}đ (giá bán - giá nhập).";
        return RedirectToPage();
    }

    private async Task TangDoanhThuBanThangAsync(string maChiNhanh, long soTienLai, int soXe, int maDong)
    {
        if (string.IsNullOrEmpty(maChiNhanh)) return;

        var kyBaoCao = DateTime.Now.ToString("yyyy-MM");
        var thongKe = await _db.ThongKeTongHop_Boss
            .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == maChiNhanh);

        if (thongKe == null)
        {
            _db.ThongKeTongHop_Boss.Add(new ThongKeTongHop_Boss
            {
                KyBaoCao = kyBaoCao,
                MaChiNhanh = maChiNhanh,
                TongDoanhThu = soTienLai,
                TongTienCocThuVe = 0,
                TongSoXeDaBan = soXe,
                SoDonCocBiHuy = 0,
                TongLuotXemWeb = 0,
                TongLuotLaiThu = 0,
                MaDongXeBanChayNhat = maDong
            });
        }
        else
        {
            thongKe.TongDoanhThu += soTienLai;
            thongKe.TongSoXeDaBan += soXe;
            if (thongKe.MaDongXeBanChayNhat == 0) thongKe.MaDongXeBanChayNhat = maDong;
        }
    }
}
