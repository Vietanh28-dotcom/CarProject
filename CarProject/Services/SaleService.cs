using CarProject.Data;
using CarProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarProject.Services;

public interface ISaleService
{
    Task<(HoaDonMuaXe HoaDon, string TenXe)> SellAsync(DonDatCoc don, string nguoiXuat);
}

public class SaleService : ISaleService
{
    private readonly AppDbContext _db;
    private readonly IRevenueService _revenue;

    public SaleService(AppDbContext db, IRevenueService revenue)
    {
        _db = db;
        _revenue = revenue;
    }

    public async Task<(HoaDonMuaXe, string)> SellAsync(DonDatCoc don, string nguoiXuat)
    {
        var listPhienBan = don.ChiTiets.Any()
            ? don.ChiTiets.Select(c => c.PhienBan).Where(p => p != null).ToList()
            : new List<PhienBanXe?> { don.PhienBan };

        var tenXe = string.Join(", ", listPhienBan.Select(p => $"{p!.DongXe?.TenDong ?? ""} {p.TenPhienBan}".Trim()));

        var existing = await _db.HoaDonMuaXe.FirstOrDefaultAsync(h => h.MaDonCoc == don.MaDonCoc);
        if (existing != null)
        {
            don.TrangThaiDonHang = "Hoàn tất";
            await _db.SaveChangesAsync();
            return (existing, tenXe);
        }

        var tongGia = (long)listPhienBan.Sum(p => p!.GiaNiemYet);
        var hoaDon = new HoaDonMuaXe
        {
            MaHoaDon = $"HD{DateTime.Now:yyMMddHHmmss}{don.MaDonCoc:D4}",
            MaDonCoc = don.MaDonCoc,
            MaKhachHang = don.MaKhachHang,
            MaPhienBan = don.MaPhienBan ?? listPhienBan.First()!.MaPhienBan,
            MaQuanLyXuat = nguoiXuat,
            MaChiNhanh = don.MaChiNhanh,
            GiaXeThucTe = tongGia,
            ThueTruocBaVaPhiLanBanh = 0,
            SoTienDuocGiam = 0,
            TongTienPhaiTra = tongGia,
            SoTienDaThanhToan = (long)don.SoTienCoc,
            PhuongThucThanhToan = don.PhuongThucThanhToan,
            NgayXuatHoaDon = DateTime.Now,
            SoKhung = "Chưa cập nhật",
            SoMay = "Chưa cập nhật",
            TrangThaiHoaDon = "Đã thanh toán"
        };
        _db.HoaDonMuaXe.Add(hoaDon);

        // Trừ tồn kho của từng phiên bản theo ĐÚNG số lượng xe đã bán (không phải 1 xe/phiên bản).
        var stockByPb = don.ChiTiets
            .Where(c => c.PhienBan != null)
            .GroupBy(c => c.MaPhienBan!)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.SoLuong));
        if (don.PhienBan != null && stockByPb.Count == 0)
            stockByPb[don.PhienBan.MaPhienBan] = 1;
        foreach (var pb in listPhienBan)
        {
            if (pb == null) continue;
            var soLuongBan = stockByPb.TryGetValue(pb.MaPhienBan, out var sl) ? sl : 1;
            if (soLuongBan > 0)
                pb.SoLuongTrongKho = Math.Max(0, pb.SoLuongTrongKho - soLuongBan);
        }

        // Trừ tồn kho vật lý theo từng showroom (chi tiết của showroom nào thì trừ showroom đó).
        foreach (var g in don.ChiTiets.Where(c => !string.IsNullOrEmpty(c.MaChiNhanh)).GroupBy(c => c.MaChiNhanh!))
        {
            foreach (var ct in g)
            {
                var tonKho = await _db.TonKhoTheoChiNhanh
                    .FirstOrDefaultAsync(t => t.MaPhienBan == ct.MaPhienBan && t.MaChiNhanh == g.Key);
                if (tonKho == null) continue;
                tonKho.SoLuong = Math.Max(0, tonKho.SoLuong - ct.SoLuong);
                tonKho.NgayCapNhat = DateTime.Now;
            }
        }

        var kyBaoCao = DateTime.Now.ToString("yyyy-MM");
        // Doanh thu bán ghi theo NHÃN (showroom nguồn đã vận chuyển xe), phần còn lại ghi cho showroom đích.
        // Với xe trong đơn đã được điều chuyển từ showroom X thì giá bán của số xe đó thuộc về X.
        var chiTiets = don.ChiTiets;
        var phienBanLut = listPhienBan.ToDictionary(p => p!.MaPhienBan, p => p!);
        // tổng số xe (theo số lượng) của từng phiên bản trong đơn
        var qtyByPb = chiTiets
            .Where(c => c != null)
            .GroupBy(c => c.MaPhienBan)
            .ToDictionary(g => g.Key, g => g.Sum(c => c.SoLuong));
        // nhãn theo từng phiên bản: phiên bản nào có bao nhiêu xe điều chuyển từ showroom nguồn nào (trạng thái đã xác nhận)
        var dieuChuyen = await _db.YeuCauDieuChuyenXe
            .Where(y => chiTiets != null && chiTiets.Select(c => c.MaChiTiet).Contains(y.MaChiTiet) && y.TrangThai == "Đã xác nhận")
            .ToListAsync();
        var labelByPb = new Dictionary<int, Dictionary<string, int>>();
        foreach (var ct in chiTiets ?? new List<DonDatCocChiTiet>())
        {
            if (ct == null) continue;
            var arr = dieuChuyen.Where(d => d.MaChiTiet == ct.MaChiTiet).ToList();
            if (arr.Count == 0) continue;
            if (!labelByPb.TryGetValue(ct.MaPhienBan, out var inner))
            {
                inner = new Dictionary<string, int>();
                labelByPb[ct.MaPhienBan] = inner;
            }
            foreach (var d in arr)
            {
                var label = d.MaChiNhanhNguon ?? ct.MaChiNhanh ?? "";
                inner.TryGetValue(label, out var cur);
                inner[label] = cur + d.SoLuong;
            }
        }

        // giá 1 xe của từng phiên bản
        long Gia1Xe(int pb) => (long)(phienBanLut.TryGetValue(pb, out var p) ? p.GiaNiemYet : 0);

        // Phân bổ tiền doanh thu theo nhãn, theo tỷ trọng xe.
        // Mỗi phiên bản đóng góp Gia1Xe * (số xe của nó). Nếu 1 phiên bản có nhiều xe do nhiều showroom cung cấp,
        // thì chia theo số lượng xe từng nhãn.
        var revenueByShowroom = new Dictionary<string, long>();
        foreach (var pb in phienBanLut.Keys)
        {
            int soXe = qtyByPb.TryGetValue(pb, out var q) ? q : 1;
            long donGia = Gia1Xe(pb);
            if (labelByPb.TryGetValue(pb, out var labels))
            {
                int trongLuongTong = Math.Max(1, labels.Values.Sum());
                foreach (var kv in labels)
                {
                    var amount = donGia * kv.Value / trongLuongTong;
                    if (amount <= 0) continue;
                    if (!revenueByShowroom.TryGetValue(kv.Key, out var a)) revenueByShowroom[kv.Key] = a;
                    revenueByShowroom[kv.Key] += amount;
                }
                // phần xe còn lại (không điều chuyển) thuộc showroom đích của đơn
                int conLai = Math.Max(0, soXe - trongLuongTong);
                if (conLai > 0)
                {
                    var amount = donGia * conLai / Math.Max(1, soXe);
                    if (amount > 0)
                    {
                        var cn = don.MaChiNhanh ?? "";
                        if (!revenueByShowroom.TryGetValue(cn, out var a)) revenueByShowroom[cn] = a;
                        revenueByShowroom[cn] += amount;
                    }
                }
            }
            else
            {
                var cn = don.MaChiNhanh ?? "";
                if (!revenueByShowroom.TryGetValue(cn, out var a)) revenueByShowroom[cn] = a;
                revenueByShowroom[cn] += donGia;
            }
        }

        foreach (var kv in revenueByShowroom)
        {
            var maChiNhanh = kv.Key;
            var soTien = kv.Value;
            var thongKe = await _db.ThongKeTongHop_Boss
                .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == maChiNhanh);
            if (thongKe == null)
            {
                var maDong = listPhienBan.FirstOrDefault()?.MaDong ?? 1;
                thongKe = new ThongKeTongHop_Boss
                {
                    KyBaoCao = kyBaoCao,
                    MaChiNhanh = maChiNhanh,
                    TongDoanhThu = soTien,
                    TongTienCocThuVe = 0,
                    TongSoXeDaBan = 1,
                    SoDonCocBiHuy = 0,
                    TongLuotXemWeb = 0,
                    TongLuotLaiThu = 0,
                    MaDongXeBanChayNhat = maDong
                };
                _db.ThongKeTongHop_Boss.Add(thongKe);
            }
            else
            {
                thongKe.TongDoanhThu += soTien;
                thongKe.TongSoXeDaBan++;
            }
        }

        don.TrangThaiDonHang = "Hoàn tất";
        await _revenue.RevertDepositRevenueAsync(don.MaDonCoc);
        await _db.SaveChangesAsync();

        return (hoaDon, tenXe);
    }
}