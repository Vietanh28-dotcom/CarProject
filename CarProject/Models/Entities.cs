using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CarProject.Models;

public class HangXe
{
    [Key]
    public int MaHang { get; set; }
    public string TenHang { get; set; }
    public string QuocGia { get; set; }
    public string? DuongDanLogo { get; set; }
}

public class DongXe
{
    [Key]
    public int MaDong { get; set; }
    public int MaHang { get; set; }
    public string TenDong { get; set; }
    public string? KieuDang { get; set; }
    public string? DuongDanAnh { get; set; }
    public bool NoiBat { get; set; }

    [ForeignKey("MaHang")]
    [ValidateNever]
    public HangXe HangXe { get; set; } = null!;
    public ICollection<PhienBanXe> PhienBanXes { get; set; } = new List<PhienBanXe>();
    public ICollection<HinhAnhXe> HinhAnhXes { get; set; } = new List<HinhAnhXe>();
}

public class PhienBanXe
{
    [Key]
    public int MaPhienBan { get; set; }
    public int MaDong { get; set; }
    public string? TenPhienBan { get; set; }
    public long GiaNiemYet { get; set; }
    public long GiaNhap { get; set; }
    public string? MauSac { get; set; }
    public string? DongCo { get; set; }
    public string? HopSo { get; set; }
    public string? LoaiNhietLieu { get; set; }
    public int SoLuongTrongKho { get; set; }
    public string? DuongDanAnh { get; set; }
    public string? MaKhuyenMai { get; set; }
    public string? TrangThai { get; set; }

    [ForeignKey("MaDong")]
    public DongXe? DongXe { get; set; }
}

// Tối giản một vài model khác từ sơ đồ để sau này mở rộng
public class TaiKhoan
{
    [Key]
    [StringLength(100)]
    public string TenDangNhap { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaTaiKhoan { get; set; }

    public string? MatKhau { get; set; }
    public string VaiTro { get; set; }
    public string TrangThai { get; set; }
    public string? TenHienThi { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
    public string? SoDienThoai { get; set; }
    public string? DiaChi { get; set; }
    public string? MaXacNhan { get; set; }
    public bool DaXacNhanEmail { get; set; }
}

public class ChiTietKhachHang
{
    [Key]
    public int MaKhachHang { get; set; }
    public string HoTen { get; set; }
    public string SoDienThoai { get; set; }
    public string DiaChi { get; set; }
    public DateTime? NgaySinh { get; set; }
}
