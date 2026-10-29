using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.ChiNhanh;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public ChiNhanhShowroom ChiNhanh { get; set; }

    public SelectList QuanLyList { get; set; }

    public EditModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        ChiNhanh = await _db.ChiNhanhShowroom.FindAsync(id);
        if (ChiNhanh == null)
            return NotFound();

        QuanLyList = new SelectList(
            await _db.TaiKhoan.Where(t => t.VaiTro == "Admin" || t.VaiTro == "Quản Lý").ToListAsync(),
            "TenDangNhap", "TenDangNhap", ChiNhanh.MaQuanLy);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        _db.ChiNhanhShowroom.Update(ChiNhanh);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa chi nhánh", $"{ChiNhanh.MaChiNhanh} - {ChiNhanh.TenChiNhanh}");
        return RedirectToPage("Index");
    }
}
