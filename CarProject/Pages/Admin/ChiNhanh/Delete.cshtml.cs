using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;
using Microsoft.EntityFrameworkCore;

namespace CarProject.Pages.Admin.ChiNhanh;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public ChiNhanhShowroom ChiNhanh { get; set; }

    public string ErrorMessage { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        ChiNhanh = await _db.ChiNhanhShowroom.FindAsync(id);
        if (ChiNhanh == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ChiNhanh == null)
            return NotFound();

        var name = ChiNhanh.TenChiNhanh;
        var ma = ChiNhanh.MaChiNhanh;
        try
        {
            _db.ChiNhanhShowroom.Remove(ChiNhanh);
            await _db.SaveChangesAsync();
            await _log.LogAsync("Admin Xóa chi nhánh", $"{ma} - {name}");
            return RedirectToPage("Index");
        }
        catch (DbUpdateException)
        {
            ErrorMessage = $"Không thể xóa \"{name}\" vì có lịch hẹn hoặc thống kê liên quan. Vui lòng xóa các dữ liệu liên quan trước.";
            return Page();
        }
    }
}
