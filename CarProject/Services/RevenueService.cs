using CarProject.Data;
using CarProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarProject.Services;

public interface IRevenueService
{
    Task AllocateDepositRevenueAsync(DonDatCoc don);
    Task RevertDepositRevenueAsync(int maDonCoc);
}

public class RevenueService : IRevenueService
{
    private readonly AppDbContext _db;

    public RevenueService(AppDbContext db)
    {
        _db = db;
    }

    // Cộng doanh thu cọc (theo từng chi tiết -> đúng showroom nguồn của chi tiết đó)
    // vào ThongKeTongHop_Boss của tháng + showroom. Tổng các showroom = đúng SoTienCoc của đơn.
    public async Task AllocateDepositRevenueAsync(DonDatCoc don)
    {
        if (don.DaTinhDoanhThu) return;

        var chiTiets = await _db.DonDatCocChiTiet
            .Where(c => c.MaDonCoc == don.MaDonCoc)
            .ToListAsync();

        var kyBaoCao = (don.NgayTaoDon == default ? DateTime.Now : don.NgayTaoDon).ToString("yyyy-MM");

        foreach (var group in chiTiets
                     .Where(c => !string.IsNullOrEmpty(c.MaChiNhanh) && c.SoTienCocPhanBo > 0)
                     .GroupBy(c => c.MaChiNhanh!))
        {
            var soTien = group.Sum(c => c.SoTienCocPhanBo);
            if (soTien <= 0) continue;

            var thongKe = await _db.ThongKeTongHop_Boss
                .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == group.Key);

            if (thongKe == null)
            {
                var firstPb = group.Select(c => c.MaPhienBan).FirstOrDefault();
                var maDong = await _db.PhienBanXe
                    .Where(p => p.MaPhienBan == firstPb)
                    .Select(p => p.MaDong)
                    .FirstOrDefaultAsync();
                if (maDong == 0) maDong = 1;

                thongKe = new ThongKeTongHop_Boss
                {
                    KyBaoCao = kyBaoCao,
                    MaChiNhanh = group.Key,
                    TongDoanhThu = 0,
                    TongTienCocThuVe = (long)Math.Round(soTien),
                    TongSoXeDaBan = 0,
                    SoDonCocBiHuy = 0,
                    TongLuotXemWeb = 0,
                    TongLuotLaiThu = 0,
                    MaDongXeBanChayNhat = maDong
                };
                _db.ThongKeTongHop_Boss.Add(thongKe);
            }
            else
            {
                thongKe.TongTienCocThuVe += (long)Math.Round(soTien);
            }
        }

        don.DaTinhDoanhThu = true;
    }

    // Hoàn tiền cọc: trừ doanh thu cọc đã cộng của đơn khỏi ThongKeTongHop_Boss
    public async Task RevertDepositRevenueAsync(int maDonCoc)
    {
        var don = await _db.DonDatCoc.FindAsync(maDonCoc);
        if (don == null || !don.DaTinhDoanhThu) return;

        var chiTiets = await _db.DonDatCocChiTiet
            .Where(c => c.MaDonCoc == maDonCoc)
            .ToListAsync();

        var kyBaoCao = (don.NgayTaoDon == default ? DateTime.Now : don.NgayTaoDon).ToString("yyyy-MM");

        foreach (var group in chiTiets
                     .Where(c => !string.IsNullOrEmpty(c.MaChiNhanh) && c.SoTienCocPhanBo > 0)
                     .GroupBy(c => c.MaChiNhanh!))
        {
            var soTien = group.Sum(c => c.SoTienCocPhanBo);
            var thongKe = await _db.ThongKeTongHop_Boss
                .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == group.Key);

            if (thongKe == null) continue;

            thongKe.TongTienCocThuVe -= (long)Math.Round(soTien);
            if (thongKe.TongTienCocThuVe < 0) thongKe.TongTienCocThuVe = 0;
        }

        don.DaTinhDoanhThu = false;
    }
}
