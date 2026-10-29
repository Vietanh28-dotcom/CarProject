using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages;

public class ShowroomModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<ChiNhanhShowroom> ChiNhanhList { get; set; } = new();
    public List<HangXe> HangXeList { get; set; } = new();
    public Dictionary<int, List<DongXe>> DongXeByHang { get; set; } = new();
    public Dictionary<int, List<PhienBanXe>> PhienBanByDong { get; set; } = new();
    public Dictionary<int, List<TonKhoTheoChiNhanh>> TonKhoTheoPhienBan { get; set; } = new();
    public Dictionary<string, string> TenChiNhanhLut { get; set; } = new();

    public ShowroomModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        ChiNhanhList = await _db.ChiNhanhShowroom
            .Include(c => c.QuanLy)
            .Where(c => c.TrangThai == "Hoạt động")
            .ToListAsync();

        HangXeList = await _db.HangXe.OrderBy(h => h.TenHang).ToListAsync();

        var dongXeList = await _db.DongXe.Include(d => d.HangXe).ToListAsync();
        DongXeByHang = dongXeList.GroupBy(d => d.MaHang)
            .ToDictionary(g => g.Key, g => g.ToList());

        var phienBanList = await _db.PhienBanXe.ToListAsync();
        PhienBanByDong = phienBanList.GroupBy(p => p.MaDong)
            .ToDictionary(g => g.Key, g => g.ToList());

        var maPhienBans = phienBanList.Select(p => p.MaPhienBan).ToList();
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

        await _log.LogAsync("Xem trang Showroom");
    }
}
