using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.DongXe;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public List<Models.DongXe> DongXeList { get; set; }

    public IndexModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        DongXeList = await _db.DongXe.Include(d => d.HangXe).ToListAsync();
        await _log.LogAsync("Admin Xem danh sách dòng xe");
    }
}
