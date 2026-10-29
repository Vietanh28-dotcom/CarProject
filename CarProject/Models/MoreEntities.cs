using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarProject.Models;

    public class DonDatCoc
{
    [Key]
    public int MaDonCoc { get; set; }
    public string? MaKhachHang { get; set; }
    public int? MaPhienBan { get; set; }
    public string? MaQuanLyDuyet { get; set; }
    public string? MaChiNhanh { get; set; }
    public decimal SoTienCoc { get; set; }
    public string? PhuongThucThanhToan { get; set; }
    public string? TrangThaiThanhToan { get; set; }
    public DateTime NgayTaoDon { get; set; }
    public DateTime? NgayHenNhanXe { get; set; }
    public string? TrangThaiDonHang { get; set; }
    public string? GhiChu { get; set; }
    public string? HoTen { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public string? MaGiaoDich { get; set; }
    public long? SepayTransactionId { get; set; }
    public bool DaTinhDoanhThu { get; set; }
    public string? DiaDiemGap { get; set; }
    public string? ToaDoGap { get; set; }

    [ForeignKey("MaKhachHang")]
    public TaiKhoan? KhachHang { get; set; }

    [ForeignKey("MaPhienBan")]
    public PhienBanXe? PhienBan { get; set; }

    [ForeignKey("MaQuanLyDuyet")]
    public TaiKhoan? QuanLyDuyet { get; set; }

    [ForeignKey("MaChiNhanh")]
    public ChiNhanhShowroom? ChiNhanh { get; set; }

    public ICollection<DonDatCocChiTiet> ChiTiets { get; set; } = new List<DonDatCocChiTiet>();
    public ICollection<PhieuNhapXe> PhieuNhaps { get; set; } = new List<PhieuNhapXe>();
}

public class DonDatCocChiTiet
{
    [Key]
    public int MaChiTiet { get; set; }
    public int MaDonCoc { get; set; }
    public int MaPhienBan { get; set; }
    public string? MaChiNhanh { get; set; }
    public int SoLuong { get; set; } = 1;
    public string TrangThaiTiepNhan { get; set; } = "Chờ xác nhận";
    public string? LyDoTuChoi { get; set; }
    public string? NguoiPhanHoi { get; set; }
    public DateTime? NgayPhanHoi { get; set; }
    public decimal SoTienCocPhanBo { get; set; }
    public int SoLuongThieu { get; set; }
    public long SoTienNhapMoiXe { get; set; }
    public bool DaNhapKho { get; set; }
    public bool CanNhapHang { get; set; }

    [ForeignKey("MaDonCoc")]
    public DonDatCoc? DonDatCoc { get; set; }

    [ForeignKey("MaPhienBan")]
    public PhienBanXe? PhienBan { get; set; }

    [ForeignKey("MaChiNhanh")]
    public ChiNhanhShowroom? ChiNhanh { get; set; }
}

public class HoaDonMuaXe
{
    [Key]
    public string? MaHoaDon { get; set; }
    public int MaDonCoc { get; set; }
    public string? MaKhachHang { get; set; }
    public int MaPhienBan { get; set; }
    public string? MaQuanLyXuat { get; set; }
    public string? MaChiNhanh { get; set; }
    public long GiaXeThucTe { get; set; }
    public long ThueTruocBaVaPhiLanBanh { get; set; }
    public long SoTienDuocGiam { get; set; }
    public long TongTienPhaiTra { get; set; }
    public long SoTienDaThanhToan { get; set; }
    public string? PhuongThucThanhToan { get; set; }
    public DateTime NgayXuatHoaDon { get; set; }
    public string? SoKhung { get; set; }
    public string? SoMay { get; set; }
    public string? TrangThaiHoaDon { get; set; }

    [ForeignKey("MaDonCoc")]
    public DonDatCoc? DonDatCoc { get; set; }

    [ForeignKey("MaChiNhanh")]
    public ChiNhanhShowroom? ChiNhanh { get; set; }
}

public class LichHenLaiThu
{
    [Key]
    public int MaLichHen { get; set; }
    public string MaKhachHang { get; set; }
    public int MaDong { get; set; }
    public string MaChiNhanh { get; set; }
    public string HoTenNguoiLai { get; set; }
    public string SoDienThoai { get; set; }
    public string SoBangLaiXe { get; set; }
    public DateTime? NgayHen { get; set; }
    public string? GioHen { get; set; }
    public string TrangThai { get; set; }
    public string YKienKhachHang { get; set; }
    [NotMapped]
    public string? MaGiaoDich { get; set; }
    [NotMapped]
    public long? SepayTransactionId { get; set; }

    [ForeignKey("MaKhachHang")]
    public TaiKhoan KhachHang { get; set; }

    [ForeignKey("MaDong")]
    public DongXe DongXe { get; set; }

    [ForeignKey("MaChiNhanh")]
    public ChiNhanhShowroom ChiNhanh { get; set; }
}

public class ChiNhanhShowroom
{
    [Key]
    public string MaChiNhanh { get; set; }
    public string TenChiNhanh { get; set; }
    public string DiaChi { get; set; }
    public string ThanhPho { get; set; }
    public string DuongDayNong { get; set; }
    public string? MaQuanLy { get; set; }
    public string TrangThai { get; set; }

    [ForeignKey("MaQuanLy")]
    public TaiKhoan? QuanLy { get; set; }
}

public class ChuongTrinhKhuyenMai
{
    [Key]
    public string MaKhuyenMai { get; set; }
    public string TieuDe { get; set; }
    public string MoTa { get; set; }
    public string LoaiGiamGia { get; set; }
    public decimal GiaTriGiam { get; set; }
    public decimal MucGiamToiDa { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayKetThuc { get; set; }
    public string TrangThai { get; set; }
}

public class QuangCaoBanner
{
    [Key]
    public int MaBanner { get; set; }
    public string DuongDanAnh { get; set; }
    public string DuongDanLienKet { get; set; }
    public int ThuTuHienThi { get; set; }
    public string MaQuanLyCapNhat { get; set; }
    public bool TrangThaiKichHoat { get; set; }

    [ForeignKey("MaQuanLyCapNhat")]
    public TaiKhoan QuanLyCapNhat { get; set; }
}

public class KenhTuVan
{
    [Key]
    public int MaKenh { get; set; }
    public string UrlMessenger { get; set; }
    public string UrlZalo { get; set; }
    public string UrlSMS { get; set; }
}

public class NhatKyHeThong
{
    [Key]
    public int MaNhatKy { get; set; }
    public string MaTaiKhoan { get; set; }
    public string TenDangNhap { get; set; }
    public string VaiTro { get; set; }
    public string HanhDong { get; set; }
    public string ChiTiet { get; set; }
    public string DiaChiIP { get; set; }
    public string TrinhDuyet { get; set; }
    public string DuongDan { get; set; }
    public DateTime ThoiGian { get; set; }

    [ForeignKey("MaTaiKhoan")]
    public TaiKhoan TaiKhoan { get; set; }
}

public class ThongKeTongHop_Boss
{
    [Key]
    public int MaThongKe { get; set; }
    public string KyBaoCao { get; set; }
    public string MaChiNhanh { get; set; }
    public long TongDoanhThu { get; set; }
    public long TongTienCocThuVe { get; set; }
    public int TongSoXeDaBan { get; set; }
    public int SoDonCocBiHuy { get; set; }
    public int TongLuotXemWeb { get; set; }
    public int TongLuotLaiThu { get; set; }
    public int MaDongXeBanChayNhat { get; set; }

    [ForeignKey("MaChiNhanh")]
    public ChiNhanhShowroom ChiNhanh { get; set; }

    [ForeignKey("MaDongXeBanChayNhat")]
    public DongXe DongXeBanChay { get; set; }
}

public class ThongBao
{
    [Key]
    public int MaThongBao { get; set; }
    public string MaNguoiNhan { get; set; }
    public string TieuDe { get; set; }
    public string NoiDung { get; set; }
    public string? DuongDan { get; set; }
    public bool DaXem { get; set; }
    public DateTime NgayTao { get; set; }

    [ForeignKey("MaNguoiNhan")]
    public TaiKhoan? NguoiNhan { get; set; }
}

public class HinhAnhXe
{
    [Key]
    public int MaHinhAnh { get; set; }
    public int MaDong { get; set; }
    public string DuongDanAnh { get; set; }
    public bool LaChinh { get; set; }
    public int ThuTu { get; set; }

    [ForeignKey("MaDong")]
    public DongXe DongXe { get; set; }
}

public class TonKhoTheoChiNhanh
{
    [Key]
    public int MaTonKho { get; set; }
    public int MaPhienBan { get; set; }
    public string MaChiNhanh { get; set; }
    public int SoLuong { get; set; }
    public DateTime NgayCapNhat { get; set; }

    [ForeignKey("MaPhienBan")]
    public PhienBanXe PhienBan { get; set; }

    [ForeignKey("MaChiNhanh")]
    public ChiNhanhShowroom ChiNhanh { get; set; }
}

public class PhieuNhapXe
{
    [Key]
    public int MaPhieuNhap { get; set; }
    public int? MaDonCoc { get; set; }
    public int MaPhienBan { get; set; }
    public string MaChiNhanh { get; set; }
    public int SoLuongNhap { get; set; }
    public long SoTienNhapMoiXe { get; set; }
    public long TongSoTienNhap { get; set; }
    public string NguoiNhap { get; set; }
    public DateTime NgayNhap { get; set; }
    public string? GhiChu { get; set; }

    [ForeignKey("MaDonCoc")]
    public DonDatCoc DonDatCoc { get; set; }

    [ForeignKey("MaPhienBan")]
    public PhienBanXe PhienBan { get; set; }

    [ForeignKey("MaChiNhanh")]
    public ChiNhanhShowroom ChiNhanh { get; set; }
}

public class GioHang
{
    [Key]
    public int MaGioHang { get; set; }    public string MaTaiKhoan { get; set; }
    public int MaPhienBan { get; set; }
    public int SoLuong { get; set; }
    public DateTime NgayTao { get; set; }

    [ForeignKey("MaTaiKhoan")]
    public TaiKhoan TaiKhoan { get; set; }

    [ForeignKey("MaPhienBan")]
    public PhienBanXe PhienBan { get; set; }
}

public class YeuCauDieuChuyenXe
{
    [Key]
    public int MaYeuCau { get; set; }
    public int MaDonCoc { get; set; }
    public int MaChiTiet { get; set; }
    public int MaPhienBan { get; set; }
    public string MaChiNhanhNguon { get; set; }
    public string MaChiNhanhDich { get; set; }
    public int SoLuong { get; set; }
    public string TrangThai { get; set; } = "Chờ xác nhận";
    public string NguoiGui { get; set; }
    public DateTime NgayGui { get; set; }
    public string? NguoiPhanHoi { get; set; }
    public DateTime? NgayPhanHoi { get; set; }
    public string? LyDoTuChoi { get; set; }

    [ForeignKey("MaDonCoc")]
    public DonDatCoc? DonDatCoc { get; set; }

    [ForeignKey("MaChiTiet")]
    public DonDatCocChiTiet? ChiTiet { get; set; }

    [ForeignKey("MaPhienBan")]
    public PhienBanXe? PhienBan { get; set; }

    [ForeignKey("MaChiNhanhNguon")]
    public ChiNhanhShowroom? ChiNhanhNguon { get; set; }

    [ForeignKey("MaChiNhanhDich")]
    public ChiNhanhShowroom? ChiNhanhDich { get; set; }
}
