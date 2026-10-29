using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.LichHen;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<LichHenLaiThu> LichHenList { get; set; }

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        LichHenList = await _db.LichHenLaiThu
            .Include(l => l.KhachHang)
            .Include(l => l.DongXe)
            .Include(l => l.ChiNhanh)
            .OrderByDescending(l => l.NgayHen)
            .ToListAsync();
        await _log.LogAsync("Admin Xem danh sách lịch hẹn");
    }
}
