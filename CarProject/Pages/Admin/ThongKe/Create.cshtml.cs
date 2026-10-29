using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.ThongKe;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.ThongKeTongHop_Boss ThongKe { get; set; }

    public SelectList ChiNhanhList { get; set; }
    public SelectList DongXeList { get; set; }

    public CreateModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        await LoadDropdownsAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        _db.ThongKeTongHop_Boss.Add(ThongKe);
        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Thêm thống kê", $"Kỳ={ThongKe.KyBaoCao}, CN={ThongKe.MaChiNhanh}, DT={ThongKe.TongDoanhThu}");
        return RedirectToPage("Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var chiNhanh = await _db.ChiNhanhShowroom.ToListAsync();
        ChiNhanhList = new SelectList(chiNhanh, "MaChiNhanh", "TenChiNhanh");

        var dongXe = await _db.DongXe.ToListAsync();
        DongXeList = new SelectList(dongXe, "MaDong", "TenDong");
    }
}
