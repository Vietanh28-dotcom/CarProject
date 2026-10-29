using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.HoaDon;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.HoaDonMuaXe HoaDon { get; set; }

    public SelectList DonCocList { get; set; }
    public SelectList KhachHangList { get; set; }
    public SelectList PhienBanList { get; set; }
    public SelectList QuanLyXuatList { get; set; }
    public SelectList ChiNhanhList { get; set; }

    public EditModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        HoaDon = await _db.HoaDonMuaXe.FindAsync(id);
        if (HoaDon == null)
            return NotFound();

        await LoadDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        var existing = await _db.HoaDonMuaXe.FindAsync(id);
        if (existing == null)
            return NotFound();

        existing.MaDonCoc = HoaDon.MaDonCoc;
        existing.MaKhachHang = HoaDon.MaKhachHang;
        existing.MaPhienBan = HoaDon.MaPhienBan;
        existing.MaQuanLyXuat = HoaDon.MaQuanLyXuat;
        existing.MaChiNhanh = HoaDon.MaChiNhanh;
        existing.GiaXeThucTe = HoaDon.GiaXeThucTe;
        existing.ThueTruocBaVaPhiLanBanh = HoaDon.ThueTruocBaVaPhiLanBanh;
        existing.SoTienDuocGiam = HoaDon.SoTienDuocGiam;
        existing.TongTienPhaiTra = HoaDon.TongTienPhaiTra;
        existing.SoTienDaThanhToan = HoaDon.SoTienDaThanhToan;
        existing.PhuongThucThanhToan = HoaDon.PhuongThucThanhToan;
        existing.NgayXuatHoaDon = HoaDon.NgayXuatHoaDon;
        existing.SoKhung = HoaDon.SoKhung;
        existing.SoMay = HoaDon.SoMay;
        existing.TrangThaiHoaDon = HoaDon.TrangThaiHoaDon;

        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa hóa đơn", $"Mã HĐ={id}, Tổng={existing.TongTienPhaiTra}");
        return RedirectToPage("Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var donCoc = await _db.DonDatCoc.ToListAsync();
        DonCocList = new SelectList(donCoc, "MaDonCoc", "MaDonCoc", HoaDon?.MaDonCoc);

        var khachHang = await _db.TaiKhoan.ToListAsync();
        KhachHangList = new SelectList(khachHang, "TenDangNhap", "TenDangNhap", HoaDon?.MaKhachHang);

        var phienBan = await _db.PhienBanXe.ToListAsync();
        PhienBanList = new SelectList(phienBan, "MaPhienBan", "TenPhienBan", HoaDon?.MaPhienBan);

        var ql = await _db.TaiKhoan.Where(t => t.VaiTro == "Admin" || t.VaiTro == "Quản Lý").ToListAsync();
        QuanLyXuatList = new SelectList(ql, "TenDangNhap", "TenDangNhap", HoaDon?.MaQuanLyXuat);

        var chiNhanh = await _db.ChiNhanhShowroom.Where(c => c.TrangThai == "Hoạt động").ToListAsync();
        ChiNhanhList = new SelectList(chiNhanh, "MaChiNhanh", "TenChiNhanh", HoaDon?.MaChiNhanh);
    }
}
