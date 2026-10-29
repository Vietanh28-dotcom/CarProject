using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.HangXe;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.HangXe HangXe { get; set; }

    public EditModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        HangXe = await _db.HangXe.FindAsync(id);
        if (HangXe == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        _db.HangXe.Update(HangXe);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa hãng xe", $"{HangXe.TenHang} (ID={HangXe.MaHang})");
        TempData["Success"] = $"Đã sửa hãng xe \"{HangXe.TenHang}\" thành công.";
        return RedirectToPage("Index");
    }
}
