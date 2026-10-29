using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.DongXe;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.DongXe DongXe { get; set; }

    public SelectList HangList { get; set; }

    public EditModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        DongXe = await _db.DongXe.FindAsync(id);
        if (DongXe == null)
            return NotFound();

        var hangList = await _db.HangXe.ToListAsync();
        HangList = new SelectList(hangList, "MaHang", "TenHang", DongXe.MaHang);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            var hangList = await _db.HangXe.ToListAsync();
            HangList = new SelectList(hangList, "MaHang", "TenHang", DongXe.MaHang);
            return Page();
        }

        _db.DongXe.Update(DongXe);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa dòng xe", $"{DongXe.TenDong} (ID={DongXe.MaDong})");
        TempData["Success"] = $"Đã sửa dòng xe \"{DongXe.TenDong}\" thành công.";
        return RedirectToPage("Index");
    }
}
