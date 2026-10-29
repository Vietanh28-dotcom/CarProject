using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.KenhTuVan;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.KenhTuVan Kenh { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Kenh = await _db.KenhTuVan.FindAsync(id);
        if (Kenh == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Kenh == null)
            return NotFound();

        var ma = Kenh.MaKenh;
        _db.KenhTuVan.Remove(Kenh);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Xóa kênh tư vấn", $"Mã kênh={ma}");
        return RedirectToPage("Index");
    }
}
