using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.ThongKe;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public Models.ThongKeTongHop_Boss ThongKe { get; set; }

    public SelectList ChiNhanhList { get; set; }
    public SelectList DongXeList { get; set; }

    public EditModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ThongKe = await _db.ThongKeTongHop_Boss.FindAsync(id);
        if (ThongKe == null)
            return NotFound();

        await LoadDropdownsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return Page();
        }

        var existing = await _db.ThongKeTongHop_Boss.FindAsync(id);
        if (existing == null)
            return NotFound();

        existing.KyBaoCao = ThongKe.KyBaoCao;
        existing.MaChiNhanh = ThongKe.MaChiNhanh;
        existing.TongDoanhThu = ThongKe.TongDoanhThu;
        existing.TongTienCocThuVe = ThongKe.TongTienCocThuVe;
        existing.TongSoXeDaBan = ThongKe.TongSoXeDaBan;
        existing.SoDonCocBiHuy = ThongKe.SoDonCocBiHuy;
        existing.TongLuotXemWeb = ThongKe.TongLuotXemWeb;
        existing.TongLuotLaiThu = ThongKe.TongLuotLaiThu;
        existing.MaDongXeBanChayNhat = ThongKe.MaDongXeBanChayNhat;

        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa thống kê", $"Kỳ={existing.KyBaoCao}, ID={id}");
        return RedirectToPage("Index");
    }

    private async Task LoadDropdownsAsync()
    {
        var chiNhanh = await _db.ChiNhanhShowroom.ToListAsync();
        ChiNhanhList = new SelectList(chiNhanh, "MaChiNhanh", "TenChiNhanh", ThongKe?.MaChiNhanh);

        var dongXe = await _db.DongXe.ToListAsync();
        DongXeList = new SelectList(dongXe, "MaDong", "TenDong", ThongKe?.MaDongXeBanChayNhat);
    }
}
