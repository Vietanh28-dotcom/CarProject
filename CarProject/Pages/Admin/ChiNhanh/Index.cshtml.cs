using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.ChiNhanh;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<ChiNhanhShowroom> ChiNhanhList { get; set; }

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        ChiNhanhList = await _db.ChiNhanhShowroom.Include(c => c.QuanLy).ToListAsync();
        await _log.LogAsync("Admin Xem danh sách chi nhánh");
    }
}
