using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.PhienBan;

public class EditModel : PageModel
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

    public EditModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        PhienBan = await _db.PhienBanXe.FindAsync(id);
        if (PhienBan == null)
            return NotFound();

        var dongList = await _db.DongXe.ToListAsync();
        DongList = new SelectList(dongList, "MaDong", "TenDong", PhienBan.MaDong);
        var kmList = await _db.ChuongTrinhKhuyenMai.ToListAsync();
        KhuyenMaiList = new SelectList(kmList, "MaKhuyenMai", "TieuDe", PhienBan.MaKhuyenMai);

        Showrooms = await _db.ChiNhanhShowroom.ToListAsync();
        var existingStocks = await _db.TonKhoTheoChiNhanh
            .Where(t => t.MaPhienBan == PhienBan.MaPhienBan)
            .ToListAsync();

        StockInputs = Showrooms.Select(c => new PhienBanStockInput
        {
            MaTonKho = existingStocks.FirstOrDefault(s => s.MaChiNhanh == c.MaChiNhanh)?.MaTonKho,
            MaChiNhanh = c.MaChiNhanh,
            SoLuong = existingStocks.FirstOrDefault(s => s.MaChiNhanh == c.MaChiNhanh)?.SoLuong ?? 0
        }).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        PhienBan.TrangThai = string.IsNullOrEmpty(PhienBan.TrangThai) ? "Còn hàng" : PhienBan.TrangThai;
        PhienBan.MaKhuyenMai = string.IsNullOrEmpty(PhienBan.MaKhuyenMai) ? "KM00" : PhienBan.MaKhuyenMai;
        PhienBan.DuongDanAnh = string.IsNullOrEmpty(PhienBan.DuongDanAnh) ? "/images/cars/default.jpg" : PhienBan.DuongDanAnh;

        if (!ModelState.IsValid)
        {
            var dongList = await _db.DongXe.ToListAsync();
            DongList = new SelectList(dongList, "MaDong", "TenDong", PhienBan.MaDong);
            var kmList = await _db.ChuongTrinhKhuyenMai.ToListAsync();
            KhuyenMaiList = new SelectList(kmList, "MaKhuyenMai", "TieuDe", PhienBan.MaKhuyenMai);
            Showrooms = await _db.ChiNhanhShowroom.ToListAsync();
            return Page();
        }

        var existing = await _db.PhienBanXe.FindAsync(PhienBan.MaPhienBan);
        if (existing == null)
            return NotFound();

        existing.MaDong = PhienBan.MaDong;
        existing.TenPhienBan = PhienBan.TenPhienBan;
        existing.GiaNiemYet = PhienBan.GiaNiemYet;
        existing.GiaNhap = PhienBan.GiaNhap;
        existing.MauSac = PhienBan.MauSac;
        existing.DongCo = PhienBan.DongCo;
        existing.HopSo = PhienBan.HopSo;
        existing.LoaiNhietLieu = PhienBan.LoaiNhietLieu;
        existing.DuongDanAnh = PhienBan.DuongDanAnh;
        existing.MaKhuyenMai = PhienBan.MaKhuyenMai;
        existing.TrangThai = PhienBan.TrangThai;
        existing.SoLuongTrongKho = StockInputs?.Sum(i => i.SoLuong) ?? existing.SoLuongTrongKho;

        var currentStocks = await _db.TonKhoTheoChiNhanh
            .Where(t => t.MaPhienBan == existing.MaPhienBan)
            .ToDictionaryAsync(t => t.MaChiNhanh);

        if (StockInputs != null)
        {
            foreach (var input in StockInputs.Where(i => !string.IsNullOrEmpty(i.MaChiNhanh)))
            {
                if (currentStocks.TryGetValue(input.MaChiNhanh, out var stockRow))
                {
                    if (input.SoLuong <= 0)
                    {
                        _db.TonKhoTheoChiNhanh.Remove(stockRow);
                    }
                    else
                    {
                        stockRow.SoLuong = input.SoLuong;
                        stockRow.NgayCapNhat = DateTime.Now;
                    }
                }
                else if (input.SoLuong > 0)
                {
                    _db.TonKhoTheoChiNhanh.Add(new TonKhoTheoChiNhanh
                    {
                        MaPhienBan = existing.MaPhienBan,
                        MaChiNhanh = input.MaChiNhanh,
                        SoLuong = input.SoLuong,
                        NgayCapNhat = DateTime.Now
                    });
                }
            }
        }

        await _db.SaveChangesAsync();
        await _log.LogAsync("Admin Sửa phiên bản", $"{existing.TenPhienBan} (ID={existing.MaPhienBan})");
        TempData["Success"] = $"Đã sửa phiên bản \"{existing.TenPhienBan}\" thành công.";
        return RedirectToPage("Index");
    }
}
