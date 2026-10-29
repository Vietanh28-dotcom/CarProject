using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.Users;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly IPasswordService _password;

    [BindProperty]
    public TaiKhoan User { get; set; }

    [BindProperty]
    public string MatKhauMoi { get; set; }

    [BindProperty]
    public string MaChiNhanhQuanLy { get; set; }

    public string Message { get; set; }
    public string MessageType { get; set; }
    public List<ChiNhanhShowroom> DanhSachChiNhanh { get; set; } = new();

    public EditModel(AppDbContext db, IActivityLogService log, IPasswordService password)
    {
        _db = db;
        _log = log;
        _password = password;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        User = await _db.TaiKhoan.FindAsync(id);
        if (User == null)
            return NotFound();

        DanhSachChiNhanh = await _db.ChiNhanhShowroom.ToListAsync();
        var cn = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == id);
        MaChiNhanhQuanLy = cn?.MaChiNhanh;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrEmpty(User.TenDangNhap))
        {
            Message = "Dữ liệu không hợp lệ.";
            MessageType = "error";
            return Page();
        }

        var existing = await _db.TaiKhoan.FindAsync(User.TenDangNhap);
        if (existing == null)
            return NotFound();

        existing.TenHienThi = User.TenHienThi;
        existing.Email = User.Email;
        existing.VaiTro = User.VaiTro;
        existing.TrangThai = User.TrangThai;
        existing.AvatarUrl = User.AvatarUrl;

        if (!string.IsNullOrWhiteSpace(MatKhauMoi))
        {
            if (MatKhauMoi.Length < 6)
            {
                Message = "Mật khẩu phải có ít nhất 6 ký tự.";
                MessageType = "error";
                DanhSachChiNhanh = await _db.ChiNhanhShowroom.ToListAsync();
                return Page();
            }
            existing.MatKhau = _password.Hash(MatKhauMoi);
        }

        await _db.SaveChangesAsync();

        // Assign showroom
        var currentCn = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == User.TenDangNhap);
        if (currentCn != null)
            currentCn.MaQuanLy = null;

        if (!string.IsNullOrEmpty(MaChiNhanhQuanLy))
        {
            var newCn = await _db.ChiNhanhShowroom.FindAsync(MaChiNhanhQuanLy);
            if (newCn != null)
                newCn.MaQuanLy = User.TenDangNhap;
        }

        // If user is no longer Quản Lý, unassign from showroom
        if (User.VaiTro != "Quản Lý")
        {
            var cn = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == User.TenDangNhap);
            if (cn != null)
                cn.MaQuanLy = null;
        }

        await _db.SaveChangesAsync();

        Message = "Cập nhật thông tin thành công.";
        MessageType = "success";
        await _log.LogAsync("Admin sửa thông tin người dùng", $"\"{existing.TenDangNhap}\"");

        DanhSachChiNhanh = await _db.ChiNhanhShowroom.ToListAsync();
        return Page();
    }
}
