using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.HoaDon;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.HoaDonMuaXe HoaDon { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        HoaDon = await _db.HoaDonMuaXe.Include(h => h.ChiNhanh).FirstOrDefaultAsync(h => h.MaHoaDon == id);
        if (HoaDon == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (HoaDon == null)
            return NotFound();

        var entity = await _db.HoaDonMuaXe.FindAsync(HoaDon.MaHoaDon);
        if (entity == null)
            return NotFound();

        var detail = $"Mã HĐ={entity.MaHoaDon}, Tổng={entity.TongTienPhaiTra}, TT={entity.TrangThaiHoaDon}";
        _db.HoaDonMuaXe.Remove(entity);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Xóa hóa đơn", detail);
        return RedirectToPage("Index");
    }
}
