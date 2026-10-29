using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.ThongKe;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.ThongKeTongHop_Boss ThongKe { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ThongKe = await _db.ThongKeTongHop_Boss.FindAsync(id);
        if (ThongKe == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ThongKe == null)
            return NotFound();

        var entity = await _db.ThongKeTongHop_Boss.FindAsync(ThongKe.MaThongKe);
        if (entity == null)
            return NotFound();

        var detail = $"Kỳ={entity.KyBaoCao}, CN={entity.MaChiNhanh}, DT={entity.TongDoanhThu}";
        _db.ThongKeTongHop_Boss.Remove(entity);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Xóa thống kê", detail);
        return RedirectToPage("Index");
    }
}
