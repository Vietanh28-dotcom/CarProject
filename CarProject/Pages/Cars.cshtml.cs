using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages;

public class CarsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<DongXe> DongXeList { get; set; } = new();
    public List<HangXe> HangXeList { get; set; } = new();
    public int TotalCount { get; set; }
    public Dictionary<int, List<TonKhoTheoChiNhanh>> TonKhoTheoPhienBan { get; set; } = new();
    public Dictionary<string, string> TenChiNhanhLut { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Brand { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? BodyType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Sort { get; set; }

    public CarsModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        HangXeList = await _db.HangXe.ToListAsync();

        var query = _db.DongXe.Include(d => d.HangXe).Include(d => d.PhienBanXes).AsQueryable();

        if (!string.IsNullOrEmpty(Search))
            query = query.Where(d => d.TenDong.Contains(Search));

        if (!string.IsNullOrEmpty(Brand) && int.TryParse(Brand, out var brandId))
            query = query.Where(d => d.MaHang == brandId);

        if (!string.IsNullOrEmpty(BodyType))
            query = query.Where(d => d.KieuDang == BodyType);

        query = Sort switch
        {
            "name_asc" => query.OrderBy(d => d.TenDong),
            "name_desc" => query.OrderByDescending(d => d.TenDong),
            _ => query.OrderBy(d => d.TenDong)
        };

        DongXeList = await query.ToListAsync();
        TotalCount = DongXeList.Count;

        var maPhienBans = DongXeList.SelectMany(d => d.PhienBanXes).Select(p => p.MaPhienBan).ToList();
        if (maPhienBans.Any())
        {
            var tonKhoList = await _db.TonKhoTheoChiNhanh
                .Include(t => t.ChiNhanh)
                .Where(t => maPhienBans.Contains(t.MaPhienBan) && t.SoLuong > 0)
                .ToListAsync();
            TonKhoTheoPhienBan = tonKhoList.GroupBy(t => t.MaPhienBan)
                .ToDictionary(g => g.Key, g => g.ToList());
            TenChiNhanhLut = await _db.ChiNhanhShowroom
                .ToDictionaryAsync(c => c.MaChiNhanh, c => c.TenChiNhanh);
        }

        var detail = $"Tìm kiếm=\"{Search}\" Hãng={Brand} Kiểu={BodyType} Sắp xếp={Sort} Kết quả={TotalCount}";
        await _log.LogAsync("Xem danh sách xe", detail);
    }
}
