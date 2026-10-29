using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.Users;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    public List<TaiKhoan> Users { get; set; } = new();
    public List<ChiNhanhShowroom> DanhSachChiNhanh { get; set; } = new();
    public Dictionary<string, string> UserShowroomMap { get; set; } = new();

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        Users = await _db.TaiKhoan.OrderBy(u => u.TenDangNhap).ToListAsync();
        DanhSachChiNhanh = await _db.ChiNhanhShowroom.ToListAsync();
        UserShowroomMap = await _db.ChiNhanhShowroom
            .Where(c => c.MaQuanLy != null)
            .GroupBy(c => c.MaQuanLy)
            .ToDictionaryAsync(g => g.Key, g => g.First().TenChiNhanh);
        await _log.LogAsync("Xem danh sách người dùng");
    }

    public async Task<IActionResult> OnPostUpdateRoleAsync(string tenDangNhap, string vaiTroMoi)
    {
        var user = await _db.TaiKhoan.FindAsync(tenDangNhap);
        if (user == null) return NotFound();

        var oldRole = user.VaiTro;
        user.VaiTro = vaiTroMoi;
        await _db.SaveChangesAsync();

        // If user is no longer Quản Lý, unassign from showroom
        if (vaiTroMoi != "Quản Lý")
        {
            var cn = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == tenDangNhap);
            if (cn != null)
            {
                cn.MaQuanLy = null;
                await _db.SaveChangesAsync();
                await _log.LogAsync("Hủy phân quyền quản lý showroom", $"\"{tenDangNhap}\": rời khỏi {cn.TenChiNhanh}");
            }
        }

        await _log.LogAsync("Đổi quyền người dùng", $"\"{user.TenDangNhap}\": {oldRole} -> {vaiTroMoi}");
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAssignShowroomAsync(string tenDangNhap, string maChiNhanh)
    {
        var user = await _db.TaiKhoan.FindAsync(tenDangNhap);
        if (user == null) return NotFound();

        // Remove from any current showroom
        var current = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == tenDangNhap);
        if (current != null)
            current.MaQuanLy = null;

        if (!string.IsNullOrEmpty(maChiNhanh))
        {
            var cn = await _db.ChiNhanhShowroom.FindAsync(maChiNhanh);
            if (cn != null)
            {
                cn.MaQuanLy = tenDangNhap;
                await _log.LogAsync("Phân quyền quản lý showroom", $"\"{tenDangNhap}\" -> {cn.TenChiNhanh}");
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBanAsync(string tenDangNhap)
    {
        var user = await _db.TaiKhoan.FindAsync(tenDangNhap);
        if (user == null) return NotFound();

        if (user.VaiTro == "Admin")
        {
            TempData["Error"] = "Không thể khóa tài khoản Admin.";
            return RedirectToPage();
        }

        user.TrangThai = "Banned";
        await _db.SaveChangesAsync();

        await _log.LogAsync("Khóa người dùng", $"\"{user.TenDangNhap}\"");
        TempData["Success"] = $"Đã khóa tài khoản \"{user.TenDangNhap}\".";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnbanAsync(string tenDangNhap)
    {
        var user = await _db.TaiKhoan.FindAsync(tenDangNhap);
        if (user == null) return NotFound();

        user.TrangThai = "Active";
        await _db.SaveChangesAsync();

        await _log.LogAsync("Mở khóa người dùng", $"\"{user.TenDangNhap}\"");
        TempData["Success"] = $"Đã mở khóa tài khoản \"{user.TenDangNhap}\".";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(string tenDangNhap)
    {
        var user = await _db.TaiKhoan.FindAsync(tenDangNhap);
        if (user == null) return NotFound();

        if (user.VaiTro == "Admin")
        {
            TempData["Error"] = "Không thể xóa tài khoản Admin.";
            return RedirectToPage();
        }

        var currentUser = User.GetJwtUserName();
        if (currentUser == tenDangNhap)
        {
            TempData["Error"] = "Không thể xóa tài khoản của chính bạn.";
            return RedirectToPage();
        }

        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            // Remove from showroom if assigned
            var cn = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == tenDangNhap);
            if (cn != null)
                cn.MaQuanLy = null;

            var logs = await _db.NhatKyHeThong.Where(n => n.MaTaiKhoan == tenDangNhap).ToListAsync();
            _db.NhatKyHeThong.RemoveRange(logs);

            var deposits = await _db.DonDatCoc.Where(d => d.MaKhachHang == tenDangNhap).ToListAsync();
            _db.DonDatCoc.RemoveRange(deposits);

            var testDrives = await _db.LichHenLaiThu.Where(l => l.MaKhachHang == tenDangNhap).ToListAsync();
            _db.LichHenLaiThu.RemoveRange(testDrives);

            var banners = await _db.QuangCaoBanner.Where(b => b.MaQuanLyCapNhat == tenDangNhap).ToListAsync();
            _db.QuangCaoBanner.RemoveRange(banners);

            _db.TaiKhoan.Remove(user);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            await _log.LogAsync("Xóa người dùng", $"\"{tenDangNhap}\"");
            TempData["Success"] = $"Đã xóa tài khoản \"{tenDangNhap}\".";
        }
        catch
        {
            await tx.RollbackAsync();
            TempData["Error"] = "Không thể xóa người dùng do dữ liệu liên quan.";
        }

        return RedirectToPage();
    }
}
