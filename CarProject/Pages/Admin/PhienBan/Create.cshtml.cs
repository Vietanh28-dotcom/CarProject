using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.PhienBan;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public PhienBanXe PhienBan { get; set; }

    [BindProperty]
    public List<PhienBanStockInput> StockInputs { get; set; } = new();

    public List<ChiNhanhShowroom> Showrooms { get; set; } = new();
    public SelectList DongList { get; set; }
    public SelectList KhuyenMaiList { get; set; }

    public CreateModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task OnGetAsync()
    {
        var dongList = await _db.DongXe.ToListAsync();
        DongList = new SelectList(dongList, "MaDong", "TenDong");
        var kmList = await _db.ChuongTrinhKhuyenMai.ToListAsync();
        KhuyenMaiList = new SelectList(kmList, "MaKhuyenMai", "TieuDe");

        Showrooms = await _db.ChiNhanhShowroom.ToListAsync();
        StockInputs = Showrooms.Select(c => new PhienBanStockInput
        {
            MaChiNhanh = c.MaChiNhanh,
            SoLuong = 0
        }).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        PhienBan.TrangThai = string.IsNullOrEmpty(PhienBan.TrangThai) ? "Còn hàng" : PhienBan.TrangThai;
        PhienBan.MaKhuyenMai = string.IsNullOrEmpty(PhienBan.MaKhuyenMai) ? "KM00" : PhienBan.MaKhuyenMai;
        PhienBan.DuongDanAnh = string.IsNullOrEmpty(PhienBan.DuongDanAnh) ? "/images/cars/default.jpg" : PhienBan.DuongDanAnh;

        if (!ModelState.IsValid)
        {
            var dongList = await _db.DongXe.ToListAsync();
            DongList = new SelectList(dongList, "MaDong", "TenDong");
            var kmList = await _db.ChuongTrinhKhuyenMai.ToListAsync();
            KhuyenMaiList = new SelectList(kmList, "MaKhuyenMai", "TieuDe");
            Showrooms = await _db.ChiNhanhShowroom.ToListAsync();
            return Page();
        }

        var totalStock = StockInputs?.Sum(i => i.SoLuong) ?? 0;
        PhienBan.SoLuongTrongKho = totalStock;

        _db.PhienBanXe.Add(PhienBan);
        await _db.SaveChangesAsync();

        if (StockInputs != null)
        {
            foreach (var input in StockInputs.Where(i => i.SoLuong > 0 && !string.IsNullOrEmpty(i.MaChiNhanh)))
            {
                _db.TonKhoTheoChiNhanh.Add(new TonKhoTheoChiNhanh
                {
                    MaPhienBan = PhienBan.MaPhienBan,
                    MaChiNhanh = input.MaChiNhanh,
                    SoLuong = input.SoLuong,
                    NgayCapNhat = DateTime.Now
                });
            }
            await _db.SaveChangesAsync();
        }

        await _log.LogAsync("Admin Thêm phiên bản", $"{PhienBan.TenPhienBan} - {PhienBan.GiaNiemYet:N0} VNĐ");
        TempData["Success"] = $"Đã thêm phiên bản \"{PhienBan.TenPhienBan}\" thành công.";
        return RedirectToPage("Index");
    }
}
