using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages;

public class TestDriveModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly CarProject.Services.INotificationService _notification;

    public TestDriveModel(AppDbContext db, IActivityLogService log, CarProject.Services.INotificationService notification)
    {
        _db = db;
        _log = log;
        _notification = notification;
    }

    [BindProperty]
    public string HoTen { get; set; } = "";

    [BindProperty]
    public string SoDienThoai { get; set; } = "";

    [BindProperty]
    public string? Email { get; set; }

    [BindProperty]
    public int MaDong { get; set; }

    [BindProperty]
    public string MaChiNhanh { get; set; } = "";

    [BindProperty]
    public string? SoBangLaiXe { get; set; }

    [BindProperty]
    public string? GhiChu { get; set; }

    [BindProperty(Name = "_ajax")]
    public bool IsAjaxSubmit { get; set; }

    public List<DongXe> DanhSachXe { get; set; } = new();
    public List<ChiNhanhShowroom> DanhSachChiNhanh { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        DanhSachXe = await _db.DongXe.Include(d => d.HangXe).ToListAsync();
        DanhSachChiNhanh = await _db.ChiNhanhShowroom.ToListAsync();
        await _log.LogAsync("Xem trang đăng ký lái thử");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        DanhSachXe = await _db.DongXe.Include(d => d.HangXe).ToListAsync();
        DanhSachChiNhanh = await _db.ChiNhanhShowroom.ToListAsync();

        var isAjaxSubmit = IsAjaxSubmit || string.Equals(Request.Form["_ajax"], "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(HoTen))
        {
            ErrorMessage = "Vui lòng nhập họ và tên.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        if (string.IsNullOrWhiteSpace(SoDienThoai))
        {
            ErrorMessage = "Vui lòng nhập số điện thoại.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        if (string.IsNullOrWhiteSpace(SoBangLaiXe))
        {
            ErrorMessage = "Vui lòng nhập số bằng lái xe.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        if (MaDong <= 0)
        {
            ErrorMessage = "Vui lòng chọn dòng xe.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        if (string.IsNullOrWhiteSpace(MaChiNhanh))
        {
            ErrorMessage = "Vui lòng chọn showroom.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        if (!DanhSachXe.Any(d => d.MaDong == MaDong))
        {
            ErrorMessage = "Dòng xe đã chọn không hợp lệ.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        if (!DanhSachChiNhanh.Any(c => c.MaChiNhanh == MaChiNhanh))
        {
            ErrorMessage = "Showroom đã chọn không hợp lệ.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        var userId = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userId))
        {
            ErrorMessage = "Vui lòng đăng nhập để gửi yêu cầu lái thử.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        var taiKhoan = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == userId);
        if (taiKhoan == null)
        {
            ErrorMessage = "Tài khoản không tồn tại trong hệ thống. Vui lòng đăng nhập lại.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        var dong = await _db.DongXe.FindAsync(MaDong);
        var showroom = await _db.ChiNhanhShowroom.FindAsync(MaChiNhanh);
        if (dong == null || showroom == null)
        {
            ErrorMessage = "Dữ liệu đã chọn không tồn tại trong hệ thống.";
            return isAjaxSubmit ? new JsonResult(new { success = false, error = ErrorMessage }) : Page();
        }

        var lichHen = new LichHenLaiThu
        {
            MaKhachHang = userId,
            MaDong = MaDong,
            MaChiNhanh = MaChiNhanh,
            HoTenNguoiLai = HoTen,
            SoDienThoai = SoDienThoai,
            SoBangLaiXe = SoBangLaiXe,
            NgayHen = null,
            GioHen = null,
            TrangThai = "Chờ xác nhận",
            YKienKhachHang = GhiChu ?? "",
            DongXe = dong,
            ChiNhanh = showroom,
            KhachHang = taiKhoan
        };

        try
        {
            _db.LichHenLaiThu.Add(lichHen);
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            await _log.LogAsync($"Lỗi khi lưu yêu cầu lái thử: {innerMessage}");
            ErrorMessage = "Không thể lưu yêu cầu lái thử. Vui lòng thử lại.";
            if (isAjaxSubmit)
            {
                return new JsonResult(new { success = false, error = ErrorMessage, detail = innerMessage });
            }
            ModelState.AddModelError(string.Empty, innerMessage);
            return Page();
        }

        await _log.LogAsync($"Gửi yêu cầu lái thử: {HoTen} - {SoDienThoai}");

        try
        {
            var manager = showroom.MaQuanLy;
            if (!string.IsNullOrEmpty(manager))
            {
                var title = "Yêu cầu lái thử mới";
                var content = $"Khách: {HoTen} ({SoDienThoai}) gửi yêu cầu lái thử {dong.TenDong}. Vui lòng chốt lịch hẹn và gửi lại cho khách.";
                var link = $"/QuanLy/LichHen?highlight={lichHen.MaLichHen}";
                await _notification.SendAsync(manager, title, content, link);
            }
            await _notification.SendAsync(taiKhoan.TenDangNhap, "Đã ghi nhận yêu cầu lái thử",
                "Yêu cầu lái thử của bạn đã được ghi nhận (miễn phí). Showroom sẽ gửi lịch hẹn cụ thể trong thời gian sớm nhất.", "/Profile");
        }
        catch
        {
            // Không chặn luồng chính nếu gửi thông báo lỗi
        }

        SuccessMessage = "Đã gửi yêu cầu lái thử thành công (miễn phí). Showroom sẽ liên hệ và chốt lịch hẹn cho bạn.";
        if (isAjaxSubmit)
        {
            return new JsonResult(new { success = true, message = SuccessMessage });
        }
        return Page();
    }
}