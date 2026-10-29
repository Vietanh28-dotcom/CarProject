using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.LichHen;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public LichHenLaiThu LichHen { get; set; }

    public SelectList KhachHangList { get; set; }
    public SelectList DongXeList { get; set; }
    public SelectList ChiNhanhList { get; set; }

    public EditModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        LichHen = await _db.LichHenLaiThu.FindAsync(id);
        if (LichHen == null)
            return NotFound();

        KhachHangList = new SelectList(
            await _db.TaiKhoan.ToListAsync(),
            "TenDangNhap", "TenDangNhap", LichHen.MaKhachHang);
        DongXeList = new SelectList(
            await _db.DongXe.ToListAsync(),
            "MaDong", "TenDong", LichHen.MaDong);
        ChiNhanhList = new SelectList(
            await _db.ChiNhanhShowroom.ToListAsync(),
            "MaChiNhanh", "TenChiNhanh", LichHen.MaChiNhanh);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var existing = await _db.LichHenLaiThu.FindAsync(LichHen.MaLichHen);
        if (existing == null)
            return NotFound();

        existing.MaKhachHang = LichHen.MaKhachHang;
        existing.MaDong = LichHen.MaDong;
        existing.MaChiNhanh = LichHen.MaChiNhanh;
        existing.HoTenNguoiLai = LichHen.HoTenNguoiLai;
        existing.SoDienThoai = LichHen.SoDienThoai;
        existing.SoBangLaiXe = LichHen.SoBangLaiXe;
        existing.NgayHen = LichHen.NgayHen;
        existing.GioHen = LichHen.GioHen;
        existing.TrangThai = LichHen.TrangThai;
        existing.YKienKhachHang = LichHen.YKienKhachHang;

        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa lịch hẹn", $"Mã={LichHen.MaLichHen}, khách={LichHen.MaKhachHang}, xe={LichHen.MaDong}");
        return RedirectToPage("Index");
    }
}
