using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;
using System.Text.RegularExpressions;

namespace CarProject.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly IPasswordService _password;

    public string ErrorMessage { get; set; }
    public bool GoogleEnabled { get; set; }
    public bool Step2 { get; set; }
    public string Email { get; set; }
    public bool DaGuiMail { get; set; }
    public bool XacNhanThanhCong { get; set; }

    public RegisterModel(AppDbContext db, IActivityLogService log, IEmailService email, IConfiguration config, IPasswordService password)
    {
        _db = db;
        _log = log;
        _email = email;
        _config = config;
        _password = password;
        GoogleEnabled = !string.IsNullOrEmpty(config["Authentication:Google:ClientId"]);
    }

    public IActionResult OnGet()
    {
        if (User.IsJwtLoggedIn())
            return RedirectToPage("/Index");

        if (Request.Query.ContainsKey("step2"))
        {
            var email = TempData.Peek("RegisterEmail") as string;
            if (!string.IsNullOrEmpty(email))
            {
                Step2 = true;
                Email = email;
            }
        }

        if (Request.Query.ContainsKey("daGuiMail"))
            DaGuiMail = true;

        if (Request.Query.ContainsKey("xacNhanThanhCong"))
            XacNhanThanhCong = true;

        return Page();
    }

    public IActionResult OnPostStep1(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            ErrorMessage = "Email không hợp lệ.";
            return Page();
        }

        var exist = _db.TaiKhoan.Any(t => t.TenDangNhap == email);
        if (exist)
        {
            return RedirectToPage("/Account/Login", new { email, daCoTaiKhoan = true });
        }

        TempData["RegisterEmail"] = email;
        return RedirectToPage(new { step2 = true });
    }

    public async Task<IActionResult> OnPostStep2Async(string password, string confirmPassword, string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            email = TempData["RegisterEmail"] as string;
            if (string.IsNullOrEmpty(email))
                return RedirectToPage();
        }

        var error = KiemTraMatKhau(password);
        if (error != null)
        {
            ErrorMessage = error;
            Step2 = true; Email = email;
            return Page();
        }

        if (password != confirmPassword)
        {
            ErrorMessage = "Mật khẩu xác nhận không khớp.";
            Step2 = true; Email = email;
            return Page();
        }

        if (await _db.TaiKhoan.AnyAsync(t => t.TenDangNhap == email))
            return RedirectToPage("/Account/Login", new { email, daCoTaiKhoan = true });

        var token = Guid.NewGuid().ToString("N");
        var user = new TaiKhoan
        {
            TenDangNhap = email,
            MatKhau = _password.Hash(password),
            VaiTro = "User",
            TrangThai = "Active",
            TenHienThi = email.Split('@')[0],
            Email = email,
            AvatarUrl = "",
            MaXacNhan = token,
            DaXacNhanEmail = false
        };
        _db.TaiKhoan.Add(user);
        var smtpHost = _config["Smtp:Host"];
        if (!string.IsNullOrEmpty(smtpHost))
        {
            user.MaXacNhan = token;
            user.DaXacNhanEmail = false;
            await _db.SaveChangesAsync();
            await _log.LogAsync("Đăng ký tài khoản", $"Email \"{email}\" — chờ xác nhận");

            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var callbackUrl = $"{baseUrl}/Account/Register?handler=XacNhan";
            await _email.GuiEmailXacNhan(email, token, callbackUrl);

            return RedirectToPage(new { daGuiMail = true });
        }

        user.MaXacNhan = null;
        user.DaXacNhanEmail = true;
        await _db.SaveChangesAsync();
        await _log.LogAsync("Đăng ký tài khoản", $"Email \"{email}\" — tự động xác nhận (không SMTP)");
        return RedirectToPage("/Account/Login", new { dangKyThanhCong = true });
    }

    public async Task<IActionResult> OnGetXacNhanAsync(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            ErrorMessage = "Link xác nhận không hợp lệ.";
            return Page();
        }

        var user = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == email && t.MaXacNhan == token);
        if (user == null)
        {
            ErrorMessage = "Link xác nhận không hợp lệ hoặc đã hết hạn.";
            return Page();
        }

        user.DaXacNhanEmail = true;
        user.MaXacNhan = null;
        await _db.SaveChangesAsync();
        await _log.LogAsync("Xác nhận email", $"Email \"{email}\" đã xác nhận");

        return RedirectToPage(new { xacNhanThanhCong = true });
    }

    private static string KiemTraMatKhau(string mk)
    {
        if (string.IsNullOrWhiteSpace(mk) || mk.Length < 8)
            return "Mật khẩu phải có ít nhất 8 ký tự.";
        if (!Regex.IsMatch(mk, @"[A-Z]"))
            return "Mật khẩu phải có ít nhất 1 chữ hoa.";
        if (!Regex.IsMatch(mk, @"[a-z]"))
            return "Mật khẩu phải có ít nhất 1 chữ thường.";
        if (!Regex.IsMatch(mk, @"[0-9]"))
            return "Mật khẩu phải có ít nhất 1 chữ số.";
        if (!Regex.IsMatch(mk, @"[!@#$%^&*()_+\-=\[\]{};':\\|,.<>\/?]"))
            return "Mật khẩu phải có ít nhất 1 ký tự đặc biệt.";
        return null;
    }
}
