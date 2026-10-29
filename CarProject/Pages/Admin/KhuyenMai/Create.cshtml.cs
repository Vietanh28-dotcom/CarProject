using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.KhuyenMai;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public ChuongTrinhKhuyenMai KhuyenMai { get; set; }

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

        _db.ChuongTrinhKhuyenMai.Add(KhuyenMai);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Thêm khuyến mãi", $"{KhuyenMai.MaKhuyenMai} - {KhuyenMai.TieuDe}");
        return RedirectToPage("Index");
    }
}
