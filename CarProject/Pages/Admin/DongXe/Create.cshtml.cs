using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.DongXe;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.DongXe DongXe { get; set; }

    public SelectList HangList { get; set; }

    public CreateModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        var hangList = await _db.HangXe.ToListAsync();
        HangList = new SelectList(hangList, "MaHang", "TenHang");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        _db.DongXe.Add(DongXe);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Thêm dòng xe", $"{DongXe.TenDong} - {DongXe.KieuDang}");
        TempData["Success"] = $"Đã thêm dòng xe \"{DongXe.TenDong}\" thành công.";
        return RedirectToPage("Index");
    }
}
