using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.DonCoc;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public DonDatCoc DonCoc { get; set; }

    public SelectList KhachHangList { get; set; }
    public SelectList PhienBanList { get; set; }

    public EditModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        DonCoc = await _db.DonDatCoc
            .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(d => d.ChiTiets).ThenInclude(c => c.ChiNhanh)
            .Include(d => d.PhieuNhaps)
            .FirstOrDefaultAsync(d => d.MaDonCoc == id);
        if (DonCoc == null)
            return NotFound();

        KhachHangList = new SelectList(
            await _db.TaiKhoan.ToListAsync(),
            "TenDangNhap", "TenDangNhap", DonCoc.MaKhachHang);
        PhienBanList = new SelectList(
            await _db.PhienBanXe.ToListAsync(),
            "MaPhienBan", "TenPhienBan", DonCoc.MaPhienBan);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var existing = await _db.DonDatCoc.FindAsync(DonCoc.MaDonCoc);
        if (existing == null)
            return NotFound();

        existing.MaKhachHang = DonCoc.MaKhachHang;
        existing.MaPhienBan = DonCoc.MaPhienBan;
        existing.SoTienCoc = DonCoc.SoTienCoc;
        existing.PhuongThucThanhToan = DonCoc.PhuongThucThanhToan;
        existing.TrangThaiThanhToan = DonCoc.TrangThaiThanhToan;
        existing.NgayTaoDon = DonCoc.NgayTaoDon;
        existing.NgayHenNhanXe = DonCoc.NgayHenNhanXe;
        existing.TrangThaiDonHang = DonCoc.TrangThaiDonHang;
        existing.GhiChu = DonCoc.GhiChu;

        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa đơn cọc", $"Mã đơn={DonCoc.MaDonCoc}, tiền={DonCoc.SoTienCoc:N0}");
        return RedirectToPage("Index");
    }

    // ----- Nhập xe vào showroom cho đơn (popup nhập hàng) -----
    public async Task<IActionResult> OnPostNhapXeAsync(int maChiTiet, int soLuongNhap, long soTienNhapMoiXe, string? ghiChu)
    {
        var chiTiet = await _db.DonDatCocChiTiet
            .Include(c => c.PhienBan)
            .FirstOrDefaultAsync(c => c.MaChiTiet == maChiTiet);
        if (chiTiet == null) return NotFound();

        if (soLuongNhap <= 0)
        {
            TempData["Error"] = "Số lượng xe nhập phải lớn hơn 0.";
            return RedirectToPage(new { id = chiTiet.MaDonCoc });
        }

        var maDonCoc = chiTiet.MaDonCoc;
        var maChiNhanh = chiTiet.MaChiNhanh ?? "";

        // 1. Tăng tồn kho của showroom nguồn
        var tonKho = await _db.TonKhoTheoChiNhanh
            .FirstOrDefaultAsync(t => t.MaPhienBan == chiTiet.MaPhienBan && t.MaChiNhanh == maChiNhanh);
        if (tonKho == null)
        {
            tonKho = new TonKhoTheoChiNhanh
            {
                MaPhienBan = chiTiet.MaPhienBan,
                MaChiNhanh = maChiNhanh,
                SoLuong = soLuongNhap,
                NgayCapNhat = DateTime.Now
            };
            _db.TonKhoTheoChiNhanh.Add(tonKho);
        }
        else
        {
            tonKho.SoLuong += soLuongNhap;
            tonKho.NgayCapNhat = DateTime.Now;
        }

        // 2. Tăng tồn kho tổng của phiên bản
        if (chiTiet.PhienBan != null)
        {
            chiTiet.PhienBan.SoLuongTrongKho += soLuongNhap;
        }

        // 3. Ghi phiếu nhập hàng
        _db.PhieuNhapXe.Add(new PhieuNhapXe
        {
            MaDonCoc = maDonCoc,
            MaPhienBan = chiTiet.MaPhienBan,
            MaChiNhanh = maChiNhanh,
            SoLuongNhap = soLuongNhap,
            SoTienNhapMoiXe = soTienNhapMoiXe,
            TongSoTienNhap = soLuongNhap * soTienNhapMoiXe,
            NguoiNhap = User.GetJwtUserName() ?? "admin",
            NgayNhap = DateTime.Now,
            GhiChu = ghiChu
        });

        // 4. Cập nhật chi tiết đơn
        chiTiet.DaNhapKho = true;
        chiTiet.SoTienNhapMoiXe = soTienNhapMoiXe;
        chiTiet.SoLuongThieu = Math.Max(0, chiTiet.SoLuongThieu - soLuongNhap);

        // 5. Trừ doanh thu showroom do nhập hàng
        await GiamDoanhThuNhapHangAsync(maChiNhanh, soLuongNhap * soTienNhapMoiXe);

        await _db.SaveChangesAsync();

        await _log.LogAsync("Admin nhập xe vào showroom",
            $"Đơn #{maDonCoc}, chi tiết #{maChiTiet}, nhập {soLuongNhap} xe vào {maChiNhanh}, giá {soTienNhapMoiXe:N0}đ/xe");

        TempData["Success"] = $"Đã nhập {soLuongNhap} xe vào showroom với giá {soTienNhapMoiXe:N0}đ/xe.";
        return RedirectToPage(new { id = maDonCoc });
    }

    private async Task GiamDoanhThuNhapHangAsync(string maChiNhanh, long soTienNhap)
    {
        if (string.IsNullOrEmpty(maChiNhanh) || soTienNhap <= 0) return;

        var kyBaoCao = DateTime.Now.ToString("yyyy-MM");
        var thongKe = await _db.ThongKeTongHop_Boss
            .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == maChiNhanh);

        if (thongKe == null)
        {
            thongKe = new ThongKeTongHop_Boss
            {
                KyBaoCao = kyBaoCao,
                MaChiNhanh = maChiNhanh,
                TongDoanhThu = 0,
                TongTienCocThuVe = 0,
                TongSoXeDaBan = 0,
                SoDonCocBiHuy = 0,
                TongLuotXemWeb = 0,
                TongLuotLaiThu = 0,
                MaDongXeBanChayNhat = 0
            };
            _db.ThongKeTongHop_Boss.Add(thongKe);
        }

        thongKe.TongDoanhThu = Math.Max(0, thongKe.TongDoanhThu - soTienNhap);
    }
}
