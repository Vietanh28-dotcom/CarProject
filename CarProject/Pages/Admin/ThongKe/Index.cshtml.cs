using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.ThongKe;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<Models.ThongKeTongHop_Boss> ThongKeList { get; set; }

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        ThongKeList = await _db.ThongKeTongHop_Boss
            .Include(t => t.ChiNhanh)
            .Include(t => t.DongXeBanChay)
            .ToListAsync();
        await _log.LogAsync("Admin Xem danh sách thống kê");
    }
}
