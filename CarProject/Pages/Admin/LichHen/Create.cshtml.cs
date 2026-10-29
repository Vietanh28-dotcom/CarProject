using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.LichHen;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public LichHenLaiThu LichHen { get; set; }

    public SelectList KhachHangList { get; set; }
    public SelectList DongXeList { get; set; }
    public SelectList ChiNhanhList { get; set; }

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
        DongXeList = new SelectList(
            await _db.DongXe.ToListAsync(),
            "MaDong", "TenDong");
        ChiNhanhList = new SelectList(
            await _db.ChiNhanhShowroom.ToListAsync(),
            "MaChiNhanh", "TenChiNhanh");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        _db.LichHenLaiThu.Add(LichHen);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Thêm lịch hẹn", $"Mã={LichHen.MaLichHen}, khách={LichHen.MaKhachHang}, xe={LichHen.MaDong}");
        return RedirectToPage("Index");
    }
}
