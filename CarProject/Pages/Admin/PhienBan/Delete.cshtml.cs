using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;
using Microsoft.EntityFrameworkCore;

namespace CarProject.Pages.Admin.PhienBan;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public PhienBanXe PhienBan { get; set; }

    public string ErrorMessage { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        PhienBan = await _db.PhienBanXe.FindAsync(id);
        if (PhienBan == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (PhienBan == null)
            return NotFound();

        var ten = PhienBan.TenPhienBan;
        var ma = PhienBan.MaPhienBan;
        try
        {
            _db.PhienBanXe.Remove(PhienBan);
            await _db.SaveChangesAsync();
            await _log.LogAsync("Admin Xóa phiên bản", $"{ten} (ID={ma})");
            TempData["Success"] = $"Đã xóa phiên bản \"{ten}\" thành công.";
            return RedirectToPage("Index");
        }
        catch (DbUpdateException)
        {
            ErrorMessage = $"Không thể xóa \"{ten}\" vì có đơn cọc hoặc hóa đơn liên quan. Vui lòng xóa các dữ liệu liên quan trước.";
            TempData["Error"] = ErrorMessage;
            return Page();
        }
    }
}
