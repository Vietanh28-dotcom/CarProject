using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.HoaDon;

public class CreateModel : PageModel
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

    public CreateModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        _db.HoaDonMuaXe.Add(HoaDon);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Thêm hóa đơn", $"Mã HĐ={HoaDon.MaHoaDon}, Khách={HoaDon.MaKhachHang}, Tổng={HoaDon.TongTienPhaiTra}");
        return RedirectToPage("Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var donCoc = await _db.DonDatCoc.ToListAsync();
        DonCocList = new SelectList(donCoc, "MaDonCoc", "MaDonCoc");

        var khachHang = await _db.TaiKhoan.ToListAsync();
        KhachHangList = new SelectList(khachHang, "TenDangNhap", "TenDangNhap");

        var phienBan = await _db.PhienBanXe.ToListAsync();
        PhienBanList = new SelectList(phienBan, "MaPhienBan", "TenPhienBan");

        var ql = await _db.TaiKhoan.Where(t => t.VaiTro == "Admin" || t.VaiTro == "Quản Lý").ToListAsync();
        QuanLyXuatList = new SelectList(ql, "TenDangNhap", "TenDangNhap");

        var chiNhanh = await _db.ChiNhanhShowroom.Where(c => c.TrangThai == "Hoạt động").ToListAsync();
        ChiNhanhList = new SelectList(chiNhanh, "MaChiNhanh", "TenChiNhanh");
    }
}
