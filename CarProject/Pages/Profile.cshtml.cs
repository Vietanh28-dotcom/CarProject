using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;
using System.Text.Json;

namespace CarProject.Pages;

public class ProfileModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly IWebHostEnvironment _env;
    private readonly IPasswordService _password;

    public ProfileModel(AppDbContext db, IActivityLogService log, IWebHostEnvironment env, IPasswordService password)
    {
        _db = db;
        _log = log;
        _env = env;
        _password = password;
    }

    public TaiKhoan TaiKhoan { get; set; }
    public string TenHienThi { get; set; }
    public string AvatarUrl { get; set; }
    public int? MaTaiKhoan { get; set; }

    public List<DonDatCoc> DonDatCocList { get; set; } = new();
    public List<LichHenLaiThu> LichHenList { get; set; } = new();
    public int SoDonCoc { get; set; }
    public int SoLichHen { get; set; }

    [BindProperty]
    public string NewTenHienThi { get; set; }

    [BindProperty]
    public string MatKhauCu { get; set; }

    [BindProperty]
    public string MatKhauMoi { get; set; }

    [BindProperty]
    public string XacNhanMatKhau { get; set; }

    public string Message { get; set; }
    public string MessageType { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.GetJwtUserName();
        if (!string.IsNullOrEmpty(userId))
            await LoadProfileDataAsync(userId);
        await _log.LogAsync("Xem trang hồ sơ cá nhân");
    }

    private async Task LoadProfileDataAsync(string userId)
    {
        TaiKhoan = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == userId);
        if (TaiKhoan != null)
        {
            TenHienThi = TaiKhoan.TenHienThi ?? TaiKhoan.TenDangNhap;
            AvatarUrl = TaiKhoan.AvatarUrl;
            MaTaiKhoan = TaiKhoan.MaTaiKhoan;
        }

        DonDatCocList = await _db.DonDatCoc
            .Include(d => d.PhienBan).ThenInclude(p => p.DongXe)
            .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(d => d.ChiNhanh)
            .Where(d => d.MaKhachHang == userId)
            .OrderByDescending(d => d.NgayTaoDon)
            .ToListAsync();

        LichHenList = await _db.LichHenLaiThu
            .Include(l => l.DongXe)
            .Where(l => l.MaKhachHang == userId)
            .OrderByDescending(l => l.NgayHen)
            .ToListAsync();

        SoDonCoc = DonDatCocList.Count;
        SoLichHen = LichHenList.Count;
    }

    public async Task<IActionResult> OnPostUpdateNameAsync()
    {
        var userId = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Account/Login");

        var user = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == userId);
        if (user != null && !string.IsNullOrWhiteSpace(NewTenHienThi))
        {
            user.TenHienThi = NewTenHienThi.Trim();
            await _db.SaveChangesAsync();
            var jwt = HttpContext.RequestServices.GetRequiredService<IJwtService>();
            HttpContext.SetJwtCookie(jwt.GenerateToken(user));
            Message = "Cập nhật tên hiển thị thành công.";
            MessageType = "success";
            await _log.LogAsync("Cập nhật tên hiển thị");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangePasswordAsync()
    {
        var userId = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Account/Login");

        if (string.IsNullOrEmpty(MatKhauCu) || string.IsNullOrEmpty(MatKhauMoi) || string.IsNullOrEmpty(XacNhanMatKhau))
        {
            Message = "Vui lòng điền đầy đủ thông tin.";
            MessageType = "error";
            await LoadProfileDataAsync(userId);
            return Page();
        }

        if (MatKhauMoi != XacNhanMatKhau)
        {
            Message = "Mật khẩu mới không khớp.";
            MessageType = "error";
            await LoadProfileDataAsync(userId);
            return Page();
        }

        if (MatKhauMoi.Length < 6)
        {
            Message = "Mật khẩu phải có ít nhất 6 ký tự.";
            MessageType = "error";
            await LoadProfileDataAsync(userId);
            return Page();
        }

        var user = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == userId);
        if (user == null) return NotFound();

        if (!_password.Verify(MatKhauCu, user.MatKhau ?? ""))
        {
            Message = "Mật khẩu cũ không đúng.";
            MessageType = "error";
            await LoadProfileDataAsync(userId);
            return Page();
        }

        user.MatKhau = _password.Hash(MatKhauMoi);
        await _db.SaveChangesAsync();

        await LoadProfileDataAsync(userId);
        Message = "Đổi mật khẩu thành công.";
        MessageType = "success";
        await _log.LogAsync("Đổi mật khẩu");
        return Page();
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostUploadAvatarAsync()
    {
        try
        {
            var userId = User.GetJwtUserName();
            if (string.IsNullOrEmpty(userId)) return RedirectToPage("/Account/Login");

            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            var logPathDebug = Path.Combine(Path.GetTempPath(), "avatar_debug.log");
            System.IO.File.AppendAllText(logPathDebug,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Body received, length={body?.Length ?? 0}\n");

            // Expect JSON: { "avatarBase64": "data:image/jpeg;base64,..." }
            var payload = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(body);
            var dataUrl = payload.GetProperty("avatarBase64").GetString();
            if (string.IsNullOrEmpty(dataUrl))
            {
                Message = "Vui lòng chọn ảnh.";
                MessageType = "error";
                return RedirectToPage();
            }

            var commaIdx = dataUrl.IndexOf(',');
            if (commaIdx < 0)
            {
                Message = "Dữ liệu ảnh không hợp lệ.";
                MessageType = "error";
                return RedirectToPage();
            }

            var mime = dataUrl.Substring(5, commaIdx - 5);
            var semiIdx = mime.IndexOf(';');
            var contentType = semiIdx > 0 ? mime.Substring(0, semiIdx) : mime;
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(contentType))
            {
                Message = "Chỉ chấp nhận file ảnh (JPEG, PNG, GIF, WebP).";
                MessageType = "error";
                return RedirectToPage();
            }

            var base64 = dataUrl.Substring(commaIdx + 1);
            var bytes = Convert.FromBase64String(base64);

            var webRoot = _env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "wwwroot");
            webRoot = Path.GetFullPath(webRoot);
            var uploadsDir = Path.Combine(webRoot, "uploads", "avatars");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            var fileName = $"{userId}.jpg";
            var filePath = Path.Combine(uploadsDir, fileName);
            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            var user = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == userId);
            if (user != null)
            {
                user.AvatarUrl = $"/uploads/avatars/{fileName}";
                await _db.SaveChangesAsync();
                var jwtSvc = HttpContext.RequestServices.GetRequiredService<IJwtService>();
                HttpContext.SetJwtCookie(jwtSvc.GenerateToken(user));
            }

            System.IO.File.AppendAllText(logPathDebug,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Avatar saved OK\n");

            Message = "Cập nhật ảnh đại diện thành công.";
            MessageType = "success";
            await _log.LogAsync("Cập nhật ảnh đại diện");

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "avatar_error.log");
            System.IO.File.AppendAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n");
            Message = $"Lỗi: {ex.Message}";
            MessageType = "error";
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
