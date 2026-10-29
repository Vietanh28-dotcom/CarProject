using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.PhienBan;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<PhienBanXe> PhienBanList { get; set; }
    public Dictionary<int, string> StockSummaryByPhienBan { get; set; } = new();

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        PhienBanList = await _db.PhienBanXe.Include(p => p.DongXe).ToListAsync();
        var stockRows = await _db.TonKhoTheoChiNhanh.Include(t => t.ChiNhanh).ToListAsync();
        StockSummaryByPhienBan = stockRows
            .GroupBy(t => t.MaPhienBan)
            .ToDictionary(
                g => g.Key,
                g => string.Join(", ", g.Select(t => $"{t.ChiNhanh?.TenChiNhanh ?? t.MaChiNhanh}: {t.SoLuong}"))
            );

        await _log.LogAsync("Admin Xem danh sách phiên bản");
    }
}
