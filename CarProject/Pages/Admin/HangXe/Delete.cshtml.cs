using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;
using Microsoft.EntityFrameworkCore;

namespace CarProject.Pages.Admin.HangXe;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.HangXe HangXe { get; set; }

    public string ErrorMessage { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
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
        if (HangXe == null)
            return NotFound();

        var name = HangXe.TenHang;
        try
        {
            _db.HangXe.Remove(HangXe);
            await _db.SaveChangesAsync();
            await _log.LogAsync("Admin Xóa hãng xe", $"{name} (ID={HangXe.MaHang})");
            TempData["Success"] = $"Đã xóa hãng xe \"{name}\" thành công.";
            return RedirectToPage("Index");
        }
        catch (DbUpdateException)
        {
            ErrorMessage = $"Không thể xóa \"{name}\" vì có dòng xe thuộc hãng này. Vui lòng xóa các dòng xe liên quan trước.";
            TempData["Error"] = ErrorMessage;
            return Page();
        }
    }
}
