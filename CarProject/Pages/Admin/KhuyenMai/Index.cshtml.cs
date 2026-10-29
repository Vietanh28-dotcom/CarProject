using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.KhuyenMai;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<ChuongTrinhKhuyenMai> KhuyenMaiList { get; set; }

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        KhuyenMaiList = await _db.ChuongTrinhKhuyenMai.ToListAsync();
        await _log.LogAsync("Admin Xem danh sách khuyến mãi");
    }
}
