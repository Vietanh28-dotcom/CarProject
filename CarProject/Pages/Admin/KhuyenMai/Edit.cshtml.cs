using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.KhuyenMai;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public ChuongTrinhKhuyenMai KhuyenMai { get; set; }

    public EditModel(AppDbContext db, IActivityLogService log)
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
        if (!ModelState.IsValid)
            return Page();

        _db.ChuongTrinhKhuyenMai.Update(KhuyenMai);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa khuyến mãi", $"{KhuyenMai.MaKhuyenMai} - {KhuyenMai.TieuDe}");
        return RedirectToPage("Index");
    }
}
