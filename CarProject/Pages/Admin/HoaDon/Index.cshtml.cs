using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.HoaDon;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<Models.HoaDonMuaXe> HoaDonList { get; set; }

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        HoaDonList = await _db.HoaDonMuaXe.Include(h => h.DonDatCoc).Include(h => h.ChiNhanh).ToListAsync();
        await _log.LogAsync("Admin Xem danh sách hóa đơn");
    }
}
