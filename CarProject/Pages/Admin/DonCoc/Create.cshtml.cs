using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.DonCoc;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public DonDatCoc DonCoc { get; set; }

    public SelectList KhachHangList { get; set; }
    public SelectList PhienBanList { get; set; }

    public CreateModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        KhachHangList = new SelectList(
            await _db.TaiKhoan.ToListAsync(),
            "TenDangNhap", "TenDangNhap");
        PhienBanList = new SelectList(
            await _db.PhienBanXe.ToListAsync(),
            "MaPhienBan", "TenPhienBan");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        _db.DonDatCoc.Add(DonCoc);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Thêm đơn cọc", $"Mã đơn={DonCoc.MaDonCoc}, tiền={DonCoc.SoTienCoc:N0}");
        return RedirectToPage("Index");
    }
}
