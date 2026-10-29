using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Services;

namespace CarProject.Pages.Admin;

public class ProfileModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly IPasswordService _password;

    public ProfileModel(AppDbContext db, IActivityLogService log, IPasswordService password)
    {
        _db = db;
        _log = log;
        _password = password;
    }

    [BindProperty]
    public string UserName { get; set; }

    [BindProperty]
    public string TenHienThi { get; set; }

    [BindProperty]
    public string Email { get; set; }

    [BindProperty]
    public string MatKhauCu { get; set; }

    [BindProperty]
    public string MatKhauMoi { get; set; }

    [BindProperty]
    public string XacNhanMatKhau { get; set; }

    [BindProperty]
    public string AvatarUrl { get; set; }

    public string VaiTro { get; set; }
    public string Message { get; set; }
    public string MessageType { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login");

        var user = await _db.TaiKhoan.FindAsync(userId);
        if (user == null)
            return RedirectToPage("/Account/Login");

        UserName = user.TenDangNhap;
        TenHienThi = user.TenHienThi ?? user.TenDangNhap;
        Email = user.Email;
        VaiTro = user.VaiTro;
        AvatarUrl = user.AvatarUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userId))
            return RedirectToPage("/Account/Login");

        var user = await _db.TaiKhoan.FindAsync(userId);
        if (user == null)
            return RedirectToPage("/Account/Login");

        if (!string.IsNullOrWhiteSpace(TenHienThi))
        {
            user.TenHienThi = TenHienThi.Trim();
            HttpContext.SetJwtCookie(HttpContext.RequestServices.GetRequiredService<IJwtService>().GenerateToken(user));
        }
        if (!string.IsNullOrWhiteSpace(Email))
        {
            user.Email = Email.Trim();
        }

        if (!string.IsNullOrWhiteSpace(AvatarUrl))
        {
            user.AvatarUrl = AvatarUrl.Trim();
        }

        HttpContext.SetJwtCookie(HttpContext.RequestServices.GetRequiredService<IJwtService>().GenerateToken(user));

        if (!string.IsNullOrWhiteSpace(MatKhauMoi) || !string.IsNullOrWhiteSpace(MatKhauCu))
        {
            if (string.IsNullOrEmpty(MatKhauCu) || string.IsNullOrEmpty(MatKhauMoi) || string.IsNullOrEmpty(XacNhanMatKhau))
            {
                Message = "Vui lòng điền đầy đủ thông tin mật khẩu.";
                MessageType = "error";
                return Page();
            }
            if (MatKhauMoi != XacNhanMatKhau)
            {
                Message = "Mật khẩu mới không khớp.";
                MessageType = "error";
                return Page();
            }
            if (MatKhauMoi.Length < 6)
            {
                Message = "Mật khẩu phải có ít nhất 6 ký tự.";
                MessageType = "error";
                return Page();
            }
            if (!_password.Verify(MatKhauCu, user.MatKhau ?? ""))
            {
                Message = "Mật khẩu cũ không đúng.";
                MessageType = "error";
                return Page();
            }
            user.MatKhau = _password.Hash(MatKhauMoi);
        }

        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin cập nhật hồ sơ cá nhân");

        Message = "Cập nhật thành công.";
        MessageType = "success";
        return Page();
    }
}
