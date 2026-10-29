using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.LichHen;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public LichHenLaiThu LichHen { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        LichHen = await _db.LichHenLaiThu
            .Include(l => l.KhachHang)
            .Include(l => l.DongXe)
            .Include(l => l.ChiNhanh)
            .FirstOrDefaultAsync(l => l.MaLichHen == id);
        if (LichHen == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (LichHen == null)
            return NotFound();

        var detail = $"Mã={LichHen.MaLichHen}, khách={LichHen.KhachHang?.TenDangNhap}, xe={LichHen.DongXe?.TenDong}";
        _db.LichHenLaiThu.Remove(LichHen);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Xóa lịch hẹn", detail);
        return RedirectToPage("Index");
    }
}
