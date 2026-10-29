using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.HangXe;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.HangXe HangXe { get; set; }

    public CreateModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        _db.HangXe.Add(HangXe);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Thêm hãng xe", $"{HangXe.TenHang} ({HangXe.QuocGia})");
        TempData["Success"] = $"Đã thêm hãng xe \"{HangXe.TenHang}\" thành công.";
        return RedirectToPage("Index");
    }
}
