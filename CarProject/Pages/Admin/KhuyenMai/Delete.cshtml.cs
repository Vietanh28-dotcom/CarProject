using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.KhuyenMai;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public ChuongTrinhKhuyenMai KhuyenMai { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        KhuyenMai = await _db.ChuongTrinhKhuyenMai.FindAsync(id);
        if (KhuyenMai == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (KhuyenMai == null)
            return NotFound();

        var name = KhuyenMai.TieuDe;
        var ma = KhuyenMai.MaKhuyenMai;
        _db.ChuongTrinhKhuyenMai.Remove(KhuyenMai);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Xóa khuyến mãi", $"{ma} - {name}");
        return RedirectToPage("Index");
    }
}
