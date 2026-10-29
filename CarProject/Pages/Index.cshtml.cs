using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<HangXe> HangXeList { get; set; } = new();
    public List<DongXe> DongXeList { get; set; } = new();
    public List<QuangCaoBanner> BannerList { get; set; } = new();
    public int TotalModels { get; set; }
    public int TotalBrands { get; set; }
    public int TotalVersions { get; set; }

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        HangXeList = await _db.HangXe.ToListAsync();
        var featured = await _db.DongXe.Include(d => d.HangXe).Include(d => d.PhienBanXes)
            .Where(d => d.NoiBat)
            .ToListAsync();
        var takeCount = (featured.Count / 12) * 12;
        DongXeList = takeCount >= 12 ? featured.Take(takeCount).ToList() : featured;
        BannerList = await _db.QuangCaoBanner.OrderBy(b => b.ThuTuHienThi).ToListAsync();
        TotalBrands = HangXeList.Count;
        TotalModels = DongXeList.Count;
        TotalVersions = await _db.PhienBanXe.CountAsync();
        await _log.LogAsync("Xem trang chủ");
    }
}
