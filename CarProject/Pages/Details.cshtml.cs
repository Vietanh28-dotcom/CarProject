using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    public DongXe Dong { get; set; }
    public HangXe HangXe { get; set; }
    public List<PhienBanXe> PhienBans { get; set; }
    public Dictionary<int, List<TonKhoTheoChiNhanh>> TonKhoTheoPhienBan { get; set; } = new();
    public Dictionary<int, Dictionary<string, int>> DaDatCocTheoPhienBanVaChiNhanh { get; set; } = new();
    public Dictionary<int, int> TongDaDatCocTheoPhienBan { get; set; } = new();
    public List<HinhAnhXe> HinhAnhXes { get; set; } = new();

    public DetailsModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Dong = await _db.DongXe.Include(d => d.HangXe).FirstOrDefaultAsync(d => d.MaDong == id);
        if (Dong == null) return NotFound();
        HangXe = Dong.HangXe;
        PhienBans = await _db.PhienBanXe.Where(p => p.MaDong == id).ToListAsync();

        var maPhienBans = PhienBans.Select(p => p.MaPhienBan).ToList();
        var tonKhoList = await _db.TonKhoTheoChiNhanh
            .Include(t => t.ChiNhanh)
            .Where(t => maPhienBans.Contains(t.MaPhienBan) && t.SoLuong > 0)
            .ToListAsync();
        TonKhoTheoPhienBan = tonKhoList.GroupBy(t => t.MaPhienBan)
            .ToDictionary(g => g.Key, g => g.ToList());

        HinhAnhXes = await _db.HinhAnhXe
            .Where(h => h.MaDong == id)
            .OrderBy(h => h.ThuTu)
            .ThenBy(h => h.MaHinhAnh)
            .ToListAsync();

        // Chỉ đếm 'đã đặt cọc' cho những đơn ĐÃ THANH TOÁN (chuyển tiền thành công).
        // Số đơn chưa thanh toán không giữ xe, không chặn người khác.
        var daDatCocList = await _db.DonDatCocChiTiet
            .Where(c => maPhienBans.Contains(c.MaPhienBan)
                && c.DonDatCoc != null
                && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                // Đơn đã kết thúc (bán xong / hủy / từ chối) không còn "giữ xe" → không tính "đã cọc"
                && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã từ chối")
            .Select(c => new { c.MaChiTiet, c.MaPhienBan, c.MaChiNhanh, c.SoLuong })
            .ToListAsync();

        // Đếm 'đã cọc' theo NHÃN (showroom nguồn đã vận chuyển): xe nào được điều chuyển từ showroom X
        // thì số xe đó được ghi "đã cọc" cho X (nhãn), phần còn lại (xe có sẵn tại đích) vẫn tính cho đích.
        var maChiTiets = daDatCocList.Select(x => x.MaChiTiet).ToList();
        var dieuChuyenDaXacNhan = await _db.YeuCauDieuChuyenXe
            .Where(y => maChiTiets.Contains(y.MaChiTiet) && y.TrangThai == "Đã xác nhận")
            .Select(y => new { y.MaChiTiet, y.MaPhienBan, y.MaChiNhanhNguon, y.SoLuong })
            .ToListAsync();
        var dcByCt = dieuChuyenDaXacNhan.GroupBy(y => y.MaChiTiet).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var x in daDatCocList)
        {
            int remaining = x.SoLuong;
            if (dcByCt.TryGetValue(x.MaChiTiet, out var dcs))
            {
                foreach (var dc in dcs)
                {
                    if (dc.SoLuong <= 0) continue;
                    var labelKey = dc.MaChiNhanhNguon ?? x.MaChiNhanh ?? "";
                    if (!DaDatCocTheoPhienBanVaChiNhanh.TryGetValue(x.MaPhienBan, out var inner))
                    {
                        inner = new Dictionary<string, int>();
                        DaDatCocTheoPhienBanVaChiNhanh[x.MaPhienBan] = inner;
                    }
                    inner.TryGetValue(labelKey, out var cur);
                    inner[labelKey] = cur + dc.SoLuong;
                    remaining -= dc.SoLuong;
                }
            }
            if (remaining > 0)
            {
                var cnKey = x.MaChiNhanh ?? "";
                if (!DaDatCocTheoPhienBanVaChiNhanh.TryGetValue(x.MaPhienBan, out var inner))
                {
                    inner = new Dictionary<string, int>();
                    DaDatCocTheoPhienBanVaChiNhanh[x.MaPhienBan] = inner;
                }
                inner.TryGetValue(cnKey, out var cur);
                inner[cnKey] = cur + remaining;
            }

            TongDaDatCocTheoPhienBan.TryGetValue(x.MaPhienBan, out var total);
            TongDaDatCocTheoPhienBan[x.MaPhienBan] = total + x.SoLuong;
        }

        // Cap tổng 'đã đặt cọc' theo tồn kho thực của từng phiên bản:
        // VD tồn 6 nhưng người dùng đặt 9 => chỉ hiển thị 'đã đặt cọc 6 xe'.
        var tongTonKho = await _db.PhienBanXe
            .Where(p => maPhienBans.Contains(p.MaPhienBan))
            .ToDictionaryAsync(p => p.MaPhienBan, p => Math.Max(0, p.SoLuongTrongKho));
        foreach (var kv in TongDaDatCocTheoPhienBan.ToList())
        {
            if (tongTonKho.TryGetValue(kv.Key, out var max))
                TongDaDatCocTheoPhienBan[kv.Key] = Math.Min(kv.Value, max);
        }

        await _log.LogAsync("Xem chi tiết xe", $"{HangXe?.TenHang} {Dong.TenDong} (ID={id})");
        return Page();
    }
}
