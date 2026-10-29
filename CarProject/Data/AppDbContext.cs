using Microsoft.EntityFrameworkCore;
using CarProject.Models;

namespace CarProject.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<HangXe> HangXe { get; set; }
    public DbSet<DongXe> DongXe { get; set; }
    public DbSet<PhienBanXe> PhienBanXe { get; set; }
    public DbSet<TaiKhoan> TaiKhoan { get; set; }
    public DbSet<ChiTietKhachHang> ChiTietKhachHang { get; set; }
    public DbSet<DonDatCoc> DonDatCoc { get; set; }
    public DbSet<DonDatCocChiTiet> DonDatCocChiTiet { get; set; }
    public DbSet<HoaDonMuaXe> HoaDonMuaXe { get; set; }
    public DbSet<LichHenLaiThu> LichHenLaiThu { get; set; }
    public DbSet<ChiNhanhShowroom> ChiNhanhShowroom { get; set; }
    public DbSet<ChuongTrinhKhuyenMai> ChuongTrinhKhuyenMai { get; set; }
    public DbSet<QuangCaoBanner> QuangCaoBanner { get; set; }
    public DbSet<KenhTuVan> KenhTuVan { get; set; }
    public DbSet<NhatKyHeThong> NhatKyHeThong { get; set; }
    public DbSet<ThongKeTongHop_Boss> ThongKeTongHop_Boss { get; set; }
    public DbSet<GioHang> GioHang { get; set; }
    public DbSet<ThongBao> ThongBao { get; set; }
    public DbSet<TonKhoTheoChiNhanh> TonKhoTheoChiNhanh { get; set; }
    public DbSet<HinhAnhXe> HinhAnhXe { get; set; }
    public DbSet<PhieuNhapXe> PhieuNhapXe { get; set; }
    public DbSet<YeuCauDieuChuyenXe> YeuCauDieuChuyenXe { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HangXe>().ToTable("HangXe");
        modelBuilder.Entity<DongXe>().ToTable("DongXe");
        modelBuilder.Entity<PhienBanXe>().ToTable("PhienBanXe_SanPham");

        modelBuilder.Entity<HangXe>().HasKey(h => h.MaHang);
        modelBuilder.Entity<DongXe>().HasKey(d => d.MaDong);
        modelBuilder.Entity<PhienBanXe>().HasKey(p => p.MaPhienBan);

        modelBuilder.Entity<TaiKhoan>().ToTable("TaiKhoan");
        modelBuilder.Entity<TaiKhoan>().HasKey(t => t.TenDangNhap);
        modelBuilder.Entity<TaiKhoan>()
            .Property(t => t.MaTaiKhoan)
            .ValueGeneratedOnAdd();

        modelBuilder.Entity<ChiTietKhachHang>().ToTable("ChiTietKhachHang");
        modelBuilder.Entity<ChiTietKhachHang>().HasKey(c => c.MaKhachHang);

        modelBuilder.Entity<DonDatCoc>().ToTable("DonDatCoc");
        modelBuilder.Entity<DonDatCoc>().HasKey(d => d.MaDonCoc);

        modelBuilder.Entity<HoaDonMuaXe>().ToTable("HoaDonMuaXe");
        modelBuilder.Entity<HoaDonMuaXe>().HasKey(hd => hd.MaHoaDon);

        modelBuilder.Entity<LichHenLaiThu>().ToTable("LichHenLaiThu");
        modelBuilder.Entity<LichHenLaiThu>().HasKey(l => l.MaLichHen);

        modelBuilder.Entity<ChiNhanhShowroom>().ToTable("ChiNhanhShowroom");
        modelBuilder.Entity<ChiNhanhShowroom>().HasKey(cn => cn.MaChiNhanh);

        modelBuilder.Entity<ChuongTrinhKhuyenMai>().ToTable("ChuongTrinhKhuyenMai");
        modelBuilder.Entity<ChuongTrinhKhuyenMai>().HasKey(k => k.MaKhuyenMai);

        modelBuilder.Entity<QuangCaoBanner>().ToTable("QuangCaoBanner");
        modelBuilder.Entity<QuangCaoBanner>().HasKey(q => q.MaBanner);

        modelBuilder.Entity<KenhTuVan>().ToTable("KenhTuVan");
        modelBuilder.Entity<KenhTuVan>().HasKey(k => k.MaKenh);

        modelBuilder.Entity<NhatKyHeThong>().ToTable("NhatKyHeThong");
        modelBuilder.Entity<NhatKyHeThong>().HasKey(n => n.MaNhatKy);

        modelBuilder.Entity<ThongKeTongHop_Boss>().ToTable("ThongKeTongHop_Boss");
        modelBuilder.Entity<ThongKeTongHop_Boss>().HasKey(t => t.MaThongKe);

        modelBuilder.Entity<DongXe>()
            .HasOne(d => d.HangXe)
            .WithMany()
            .HasForeignKey(d => d.MaHang)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PhienBanXe>()
            .HasOne(p => p.DongXe)
            .WithMany(d => d.PhienBanXes)
            .HasForeignKey(p => p.MaDong)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LichHenLaiThu>()
            .HasOne(l => l.KhachHang)
            .WithMany()
            .HasForeignKey(l => l.MaKhachHang)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<LichHenLaiThu>()
            .HasOne(l => l.DongXe)
            .WithMany()
            .HasForeignKey(l => l.MaDong)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LichHenLaiThu>()
            .HasOne(l => l.ChiNhanh)
            .WithMany()
            .HasForeignKey(l => l.MaChiNhanh)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DonDatCoc>()
            .HasOne(d => d.KhachHang)
            .WithMany()
            .HasForeignKey(d => d.MaKhachHang)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DonDatCoc>()
            .HasOne(d => d.PhienBan)
            .WithMany()
            .HasForeignKey(d => d.MaPhienBan)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DonDatCoc>()
            .HasOne(d => d.QuanLyDuyet)
            .WithMany()
            .HasForeignKey(d => d.MaQuanLyDuyet)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DonDatCoc>()
            .HasOne(d => d.ChiNhanh)
            .WithMany()
            .HasForeignKey(d => d.MaChiNhanh)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DonDatCocChiTiet>().ToTable("DonDatCocChiTiet");
        modelBuilder.Entity<DonDatCocChiTiet>().HasKey(c => c.MaChiTiet);

        modelBuilder.Entity<DonDatCoc>()
            .HasMany(d => d.ChiTiets)
            .WithOne(c => c.DonDatCoc)
            .HasForeignKey(c => c.MaDonCoc)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DonDatCocChiTiet>()
            .HasOne(c => c.PhienBan)
            .WithMany()
            .HasForeignKey(c => c.MaPhienBan)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DonDatCocChiTiet>()
            .HasOne(c => c.ChiNhanh)
            .WithMany()
            .HasForeignKey(c => c.MaChiNhanh)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PhieuNhapXe>().ToTable("PhieuNhapXe");
        modelBuilder.Entity<PhieuNhapXe>().HasKey(p => p.MaPhieuNhap);

        modelBuilder.Entity<PhieuNhapXe>()
            .HasOne(p => p.DonDatCoc)
            .WithMany(d => d.PhieuNhaps)
            .HasForeignKey(p => p.MaDonCoc)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PhieuNhapXe>()
            .HasOne(p => p.PhienBan)
            .WithMany()
            .HasForeignKey(p => p.MaPhienBan)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PhieuNhapXe>()
            .HasOne(p => p.ChiNhanh)
            .WithMany()
            .HasForeignKey(p => p.MaChiNhanh)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<YeuCauDieuChuyenXe>().ToTable("YeuCauDieuChuyenXe");
        modelBuilder.Entity<YeuCauDieuChuyenXe>().HasKey(y => y.MaYeuCau);

        modelBuilder.Entity<YeuCauDieuChuyenXe>()
            .HasOne(y => y.DonDatCoc)
            .WithMany()
            .HasForeignKey(y => y.MaDonCoc)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<YeuCauDieuChuyenXe>()
            .HasOne(y => y.ChiTiet)
            .WithMany()
            .HasForeignKey(y => y.MaChiTiet)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<YeuCauDieuChuyenXe>()
            .HasOne(y => y.PhienBan)
            .WithMany()
            .HasForeignKey(y => y.MaPhienBan)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<YeuCauDieuChuyenXe>()
            .HasOne(y => y.ChiNhanhNguon)
            .WithMany()
            .HasForeignKey(y => y.MaChiNhanhNguon)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<YeuCauDieuChuyenXe>()
            .HasOne(y => y.ChiNhanhDich)
            .WithMany()
            .HasForeignKey(y => y.MaChiNhanhDich)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HoaDonMuaXe>()
            .HasOne(h => h.DonDatCoc)
            .WithMany()
            .HasForeignKey(h => h.MaDonCoc)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HoaDonMuaXe>()
            .HasOne(h => h.ChiNhanh)
            .WithMany()
            .HasForeignKey(h => h.MaChiNhanh)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<DonDatCoc>()
            .HasOne(d => d.PhienBan)
            .WithMany()
            .HasForeignKey(d => d.MaPhienBan)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChiNhanhShowroom>()
            .HasOne(c => c.QuanLy)
            .WithMany()
            .HasForeignKey(c => c.MaQuanLy)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<QuangCaoBanner>()
            .HasOne(q => q.QuanLyCapNhat)
            .WithMany()
            .HasForeignKey(q => q.MaQuanLyCapNhat)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<NhatKyHeThong>()
            .HasOne(n => n.TaiKhoan)
            .WithMany()
            .HasForeignKey(n => n.MaTaiKhoan)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThongKeTongHop_Boss>()
            .HasOne(t => t.ChiNhanh)
            .WithMany()
            .HasForeignKey(t => t.MaChiNhanh)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<ThongKeTongHop_Boss>()
            .HasOne(t => t.DongXeBanChay)
            .WithMany()
            .HasForeignKey(t => t.MaDongXeBanChayNhat)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<GioHang>().ToTable("GioHang");
        modelBuilder.Entity<GioHang>().HasKey(g => g.MaGioHang);

        modelBuilder.Entity<GioHang>()
            .HasOne(g => g.TaiKhoan)
            .WithMany()
            .HasForeignKey(g => g.MaTaiKhoan)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<GioHang>()
            .HasOne(g => g.PhienBan)
            .WithMany()
            .HasForeignKey(g => g.MaPhienBan)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ThongBao>().ToTable("ThongBao");
        modelBuilder.Entity<ThongBao>().HasKey(t => t.MaThongBao);

        modelBuilder.Entity<ThongBao>()
            .HasOne(t => t.NguoiNhan)
            .WithMany()
            .HasForeignKey(t => t.MaNguoiNhan)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TonKhoTheoChiNhanh>().ToTable("TonKhoTheoChiNhanh");
        modelBuilder.Entity<TonKhoTheoChiNhanh>().HasKey(t => t.MaTonKho);

        modelBuilder.Entity<TonKhoTheoChiNhanh>()
            .HasOne(t => t.PhienBan)
            .WithMany()
            .HasForeignKey(t => t.MaPhienBan)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TonKhoTheoChiNhanh>()
            .HasOne(t => t.ChiNhanh)
            .WithMany()
            .HasForeignKey(t => t.MaChiNhanh)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<HinhAnhXe>().ToTable("HinhAnhXe");
        modelBuilder.Entity<HinhAnhXe>().HasKey(h => h.MaHinhAnh);

        modelBuilder.Entity<HinhAnhXe>()
            .HasOne(h => h.DongXe)
            .WithMany(d => d.HinhAnhXes)
            .HasForeignKey(h => h.MaDong)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
