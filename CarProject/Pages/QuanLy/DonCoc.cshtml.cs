using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.QuanLy;

public class DonCocModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly INotificationService _notif;
    private readonly ISaleService _sale;
    private readonly IRevenueService _revenue;

    public DonCocModel(AppDbContext db, IActivityLogService log, INotificationService notif, ISaleService sale, IRevenueService revenue)
    {
        _db = db;
        _log = log;
        _notif = notif;
        _sale = sale;
        _revenue = revenue;
    }

    public ChiNhanhShowroom? Showroom { get; set; }
    public List<DonDatCoc> DanhSachDonCoc { get; set; } = new();
    public List<HoaDonMuaXe> HoaDonList { get; set; } = new();
    public List<YeuCauDieuChuyenXe> DanhSachYeuCauDen { get; set; } = new();
    public List<YeuCauDieuChuyenXe> DanhSachYeuCauDi { get; set; } = new();
    public Dictionary<int, List<SourceShowroomOption>> ShowroomNguonTheoChiTiet { get; set; } = new();
    public Dictionary<int, List<MissingCarItem>> ThieuXeTheoDon { get; set; } = new();
    public string? SuccessMessage { get; set; }
    public string? ErrorMessage { get; set; }

    private readonly Dictionary<string, int> _tonKhoYeuCau = new();

    public int TonKhoCuaShowroom(int maPhienBan, string? maChiNhanh)
        => _tonKhoYeuCau.TryGetValue($"{maPhienBan}|{maChiNhanh}", out var s) ? s : 0;

    public class SourceShowroomOption
    {
        public string MaChiNhanh { get; set; } = "";
        public string TenChiNhanh { get; set; } = "";
        public int CoSan { get; set; }
        public int CoTheDieuChuyen { get; set; }
    }

    public class MissingCarItem
    {
        public int MaChiTiet { get; set; }
        public string TenXe { get; set; } = "";
        public int SoLuongThieu { get; set; }
        public List<SourceShowroomOption> Showrooms { get; set; } = new();
        public List<DaYeuCauItem> DaGui { get; set; } = new();
    }

    public class DaYeuCauItem
    {
        public string MaChiNhanh { get; set; } = "";
        public string TenChiNhanh { get; set; } = "";
        public int SoLuong { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName))
            return RedirectToPage("/Account/Login");

        Showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (Showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return Page();
        }

        DanhSachDonCoc = await _db.DonDatCoc
            .Include(d => d.PhienBan).ThenInclude(p => p.DongXe)
            .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(d => d.ChiTiets).ThenInclude(c => c.ChiNhanh)
            .Include(d => d.KhachHang)
            .Where(d => d.ChiTiets.Any(c => c.MaChiNhanh == Showroom.MaChiNhanh))
            .OrderByDescending(d => d.NgayTaoDon)
            .ToListAsync();

        var maDons = DanhSachDonCoc.Select(d => d.MaDonCoc).ToList();
        HoaDonList = await _db.HoaDonMuaXe
            .Where(h => maDons.Contains(h.MaDonCoc))
            .ToListAsync();

        // Yêu cầu điều chuyển liên quan showroom của tôi (gửi đến hoặc tôi gửi đi) — nạp TẤT CẢ trạng thái
        // để QL xem được lịch sử và tồn kho 2 phía sau khi đã xác nhận/hoàn tất.
        DanhSachYeuCauDen = await _db.YeuCauDieuChuyenXe
            .Include(y => y.DonDatCoc)
            .Include(y => y.ChiTiet).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(y => y.ChiTiet).ThenInclude(c => c.ChiNhanh)
            .Include(y => y.ChiNhanhNguon)
            .Include(y => y.ChiNhanhDich)
            .Where(y => y.MaChiNhanhNguon == Showroom.MaChiNhanh)
            .OrderByDescending(y => y.NgayGui)
            .ToListAsync();

        // Yêu cầu điều chuyển TÔI đã gửi đi (showroom của tôi là đích/người xin xe).
        DanhSachYeuCauDi = await _db.YeuCauDieuChuyenXe
            .Include(y => y.DonDatCoc)
            .Include(y => y.ChiTiet).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(y => y.ChiTiet).ThenInclude(c => c.ChiNhanh)
            .Include(y => y.ChiNhanhNguon)
            .Include(y => y.ChiNhanhDich)
            .Where(y => y.MaChiNhanhDich == Showroom.MaChiNhanh
                && y.MaChiNhanhNguon != Showroom.MaChiNhanh)
            .OrderByDescending(y => y.NgayGui)
            .ToListAsync();

        // Tồn kho mỗi showroom cho từng phiên bản liên quan đến yêu cầu điều chuyển (để hiện "showroom mình có / showroom kia có")
        var maPbYeuCau = DanhSachYeuCauDen.Select(y => y.MaPhienBan)
            .Union(DanhSachYeuCauDi.Select(y => y.MaPhienBan))
            .Distinct()
            .ToList();
        var tonKhoYeuCau = await _db.TonKhoTheoChiNhanh
            .Include(t => t.ChiNhanh)
            .Where(t => maPbYeuCau.Contains(t.MaPhienBan))
            .ToListAsync();
        foreach (var tk in tonKhoYeuCau)
            _tonKhoYeuCau[$"{tk.MaPhienBan}|{tk.MaChiNhanh}"] = tk.SoLuong;

        // Dựng danh sách showroom nguồn khả dụng cho từng chi tiết cần điều chuyển (CanNhapHang = true)
        var chiTietCanDieuChuyen = DanhSachDonCoc
            .SelectMany(d => d.ChiTiets)
            .Where(c => c.MaChiNhanh == Showroom.MaChiNhanh && c.CanNhapHang && c.TrangThaiTiepNhan == "Chờ xác nhận")
            .ToList();
        var maPbCan = chiTietCanDieuChuyen.Select(c => c.MaPhienBan).Distinct().ToList();
        if (maPbCan.Any())
        {
            var allTonKho = await _db.TonKhoTheoChiNhanh
                .Include(t => t.ChiNhanh)
                .Where(t => maPbCan.Contains(t.MaPhienBan))
                .ToListAsync();
            foreach (var ct in chiTietCanDieuChuyen)
            {
                var options = new List<SourceShowroomOption>();
foreach (var tk in allTonKho.Where(t => t.MaPhienBan == ct.MaPhienBan && t.MaChiNhanh != Showroom.MaChiNhanh))
                {
                    // QL có thể xin điều chuyển tới hết tồn kho của showroom nguồn;
                    // số liệu chính xác sẽ do quản lý showroom đó xác nhận từ chối/đồng ý.
                    if (tk.SoLuong > 0)
                    {
                        options.Add(new SourceShowroomOption
                        {
                            MaChiNhanh = tk.MaChiNhanh,
                            TenChiNhanh = tk.ChiNhanh?.TenChiNhanh ?? tk.MaChiNhanh,
                            CoSan = tk.SoLuong,
                            CoTheDieuChuyen = tk.SoLuong
                        });
                    }
                }
                ShowroomNguonTheoChiTiet[ct.MaChiTiet] = options;
            }
        }

        // Nhóm theo đơn các xe còn thiếu mà showroom của mình cần xử lý (liệt kê cho modal nhập hàng/điều chuyển)
        // Đồng thời ghi lại các yêu cầu điều chuyển đang chờ (TrangThai="Chờ xác nhận") cho từng chi tiết,
        // để QL thấy đã yêu cầu bao nhiêu từ showroom nào và không bị mất thông tin khi bấm lại nút.
        var yeuCauChoTruoc = DanhSachYeuCauDi
            .Where(y => y.TrangThai == "Chờ xác nhận")
            .GroupBy(y => y.MaChiTiet)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(y => y.MaChiNhanhNguon)
                      .Select(x => new DaYeuCauItem
                      {
                          MaChiNhanh = x.Key,
                          TenChiNhanh = x.First().ChiNhanhNguon?.TenChiNhanh ?? x.Key,
                          SoLuong = x.Sum(y => y.SoLuong)
                      })
                      .ToList());

        foreach (var d in DanhSachDonCoc)
        {
            var items = d.ChiTiets
                .Where(c => c.MaChiNhanh == Showroom.MaChiNhanh
                    && c.CanNhapHang
                    && c.SoLuongThieu > 0
                    && c.TrangThaiTiepNhan == "Chờ xác nhận")
                .Select(c => new MissingCarItem
                {
                    MaChiTiet = c.MaChiTiet,
                    TenXe = $"{c.PhienBan?.DongXe?.TenDong ?? ""} {c.PhienBan?.TenPhienBan ?? ""}".Trim(),
                    SoLuongThieu = c.SoLuongThieu,
                    Showrooms = ShowroomNguonTheoChiTiet.TryGetValue(c.MaChiTiet, out var s) ? s : new(),
                    DaGui = yeuCauChoTruoc.TryGetValue(c.MaChiTiet, out var dg) ? dg : new()
                })
                .ToList();
            if (items.Any()) ThieuXeTheoDon[d.MaDonCoc] = items;
        }

        return Page();
    }

    // ----- Chấp nhận tiếp nhận một xe trong đơn -----
    // Khi QL bấm "Tiếp nhận": kiểm tra số xe thực tế còn lại tại chính showroom của xe đó.
    //  - Đủ xe  -> tiếp nhận bình thường.
    //  - Không đủ -> KHÔNG tiếp nhận, đánh dấu cần nhập hàng (CanNhapHang=true) + ghi lại số thiếu.
    public async Task<IActionResult> OnPostAcceptAsync(int maChiTiet)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var chiTiet = await _db.DonDatCocChiTiet
            .Include(c => c.DonDatCoc)
            .Include(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(c => c.ChiNhanh)
            .FirstOrDefaultAsync(c => c.MaChiTiet == maChiTiet);
        if (chiTiet == null) return NotFound();

        // Chỉ quản lý showroom nguồn của xe đó mới được xác nhận
        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null || chiTiet.MaChiNhanh != showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền xác nhận xe thuộc showroom khác.";
            return RedirectToPage();
        }

        if (chiTiet.TrangThaiTiepNhan != "Chờ xác nhận")
        {
            ErrorMessage = "Xe này không ở trạng thái cần tiếp nhận.";
            return RedirectToPage();
        }

        // Kiểm tra tồn kho thực tế tại showroom này (đã trừ xe đã giữ cho đơn cọc khác đã thanh toán)
        var avail = await TonKhoConLaiCuaShowroomAsync(chiTiet.MaPhienBan, chiTiet.MaChiNhanh ?? "", excludeMaChiTiet: maChiTiet);

        if (avail < chiTiet.SoLuong)
        {
            chiTiet.CanNhapHang = true;
            chiTiet.SoLuongThieu = chiTiet.SoLuong - avail;
            await _db.SaveChangesAsync();

            ErrorMessage = $"Showroom {showroom.TenChiNhanh} hiện chỉ còn {Math.Max(0, avail)} xe {PrettyTenXe(chiTiet)} nhưng đơn cần {chiTiet.SoLuong} xe. Không đủ xe để tiếp nhận. Vui lòng Nhập hàng hoặc Nhập hàng từ showroom khác.";
            return RedirectToPage();
        }

        chiTiet.TrangThaiTiepNhan = "Đã tiếp nhận";
        chiTiet.CanNhapHang = false;
        chiTiet.SoLuongThieu = 0;
        chiTiet.NguoiPhanHoi = userName;
        chiTiet.NgayPhanHoi = DateTime.Now;

        // Tiếp nhận = xe đã được cam kết/giữ cho khách → trừ HẲN tồn kho vật lý của showroom
        // và tổng tồn kho của phiên bản, theo đúng số lượng xe trong chi tiết đơn.
        await CongTruTonKhoAsync(chiTiet.MaPhienBan, chiTiet.MaChiNhanh ?? "", -chiTiet.SoLuong);
        if (chiTiet.PhienBan != null)
        {
            chiTiet.PhienBan.SoLuongTrongKho = Math.Max(0, chiTiet.PhienBan.SoLuongTrongKho - chiTiet.SoLuong);
        }

        await _db.SaveChangesAsync();

        await _log.LogAsync($"QL chấp nhận xe #{maChiTiet} của đơn #{chiTiet.MaDonCoc}");
        SuccessMessage = "Đã tiếp nhận xe trong đơn.";
        return RedirectToPage();
    }

    // ----- Từ chối tiếp nhận một xe (phải điền lý do) -----
    public async Task<IActionResult> OnPostRejectAsync(int maChiTiet, string lyDoTuChoi)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        if (string.IsNullOrWhiteSpace(lyDoTuChoi))
        {
            ErrorMessage = "Vui lòng nhập lý do từ chối.";
            return RedirectToPage();
        }

        var chiTiet = await _db.DonDatCocChiTiet
            .Include(c => c.DonDatCoc)
            .Include(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(c => c.ChiNhanh)
            .FirstOrDefaultAsync(c => c.MaChiTiet == maChiTiet);
        if (chiTiet == null) return NotFound();

        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null || chiTiet.MaChiNhanh != showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền xác nhận xe thuộc showroom khác.";
            return RedirectToPage();
        }

        chiTiet.TrangThaiTiepNhan = "Từ chối";
        chiTiet.LyDoTuChoi = lyDoTuChoi.Trim();
        chiTiet.NguoiPhanHoi = userName;
        chiTiet.NgayPhanHoi = DateTime.Now;
        await _db.SaveChangesAsync();

        var tenXe = $"{chiTiet.PhienBan?.DongXe?.TenDong ?? ""} {chiTiet.PhienBan?.TenPhienBan ?? ""}".Trim();
        await _log.LogAsync($"QL từ chối xe #{maChiTiet} đơn #{chiTiet.MaDonCoc}",
            $"Lý do: {lyDoTuChoi.Trim()}");

        // Gửi thông báo tới Admin khi từ chối
        await _notif.SendToRoleAsync("Admin", "Showroom từ chối tiếp nhận xe",
            $"Showroom {showroom.TenChiNhanh} từ chối tiếp nhận xe {tenXe} (Đơn cọc #{chiTiet.MaDonCoc}). Lý do: {lyDoTuChoi.Trim()}",
            $"/Admin/DonCoc/Edit/{chiTiet.MaDonCoc}");

        if (chiTiet.DonDatCoc?.MaKhachHang != null)
        {
            await _notif.SendAsync(chiTiet.DonDatCoc.MaKhachHang, "Xe bị từ chối tiếp nhận",
                $"Xe {tenXe} trong đơn cọc #{chiTiet.MaDonCoc} bị từ chối bởi showroom {showroom.TenChiNhanh}. Lý do: {lyDoTuChoi.Trim()}",
                "");
        }

        SuccessMessage = "Đã từ chối tiếp nhận xe. Lý do đã được gửi tới Admin.";
        return RedirectToPage();
    }

    // ----- Tiếp nhận toàn bộ đơn cọc (Quản Lý showroom nhận xe xác nhận) -----
    public async Task<IActionResult> OnPostAcceptOrderAsync(int maDonCoc, string? ngayHenNhanXe = null)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var don = await _db.DonDatCoc
            .Include(d => d.ChiTiets)
            .Include(d => d.KhachHang)
            .FirstOrDefaultAsync(d => d.MaDonCoc == maDonCoc);
        if (don == null) return NotFound();

        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null || don.MaChiNhanh != showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền tiếp nhận đơn cọc của showroom khác.";
            return RedirectToPage();
        }

        if (don.TrangThaiDonHang != "Chờ xác nhận")
        {
            ErrorMessage = "Đơn cọc không ở trạng thái cần tiếp nhận.";
            return RedirectToPage();
        }

        // Đơn tiền mặt: phải XÁC NHẬN ĐÃ NHẬN TIỀN CỌC trước khi tiếp nhận xe
        if (string.Equals(don.PhuongThucThanhToan, "Tiền mặt", StringComparison.OrdinalIgnoreCase)
            && don.TrangThaiThanhToan != "Đã thanh toán")
        {
            ErrorMessage = "Vui lòng xác nhận đã nhận tiền cọc trước khi tiếp nhận xe.";
            return RedirectToPage();
        }

        if (!don.ChiTiets.Any() || !don.ChiTiets.All(c => c.TrangThaiTiepNhan == "Đã tiếp nhận"))
        {
            ErrorMessage = "Chưa đủ điều kiện tiếp nhận: vẫn còn xe chưa được các showroom xác nhận.";
            return RedirectToPage();
        }

        // QL chốt giờ hẹn gửi lại khách khi tiếp nhận
        if (string.IsNullOrWhiteSpace(ngayHenNhanXe) || !DateTime.TryParse(ngayHenNhanXe, out var ngayHenChot))
        {
            ErrorMessage = "Vui lòng chốt ngày/giờ hẹn gặp trước khi tiếp nhận đơn cọc.";
            return RedirectToPage();
        }
        if (ngayHenChot < DateTime.Now.AddMinutes(-1))
        {
            ErrorMessage = "Không thể chọn thời điểm trong quá khứ. Vui lòng chọn giờ hẹn trong tương lai.";
            return RedirectToPage();
        }
        // Giờ hành chính 08:00 – 17:30, và phải ít nhất 3 tiếng trước khi hết giờ hành chính (tối đa 14:30)
        var trongGioHanhChinh = ngayHenChot.Hour >= 8
            && (ngayHenChot.Hour < 14 || (ngayHenChot.Hour == 14 && ngayHenChot.Minute <= 30));
        if (!trongGioHanhChinh)
        {
            ErrorMessage = "Giờ hẹn phải nằm trong giờ hành chính (08:00) và phải ít nhất 3 tiếng trước giờ kết thúc (tối đa 14:30). Vui lòng chọn lại.";
            return RedirectToPage();
        }
        don.NgayHenNhanXe = ngayHenChot;
        don.TrangThaiDonHang = "Đã xác nhận";
        don.MaQuanLyDuyet = userName;
        await _db.SaveChangesAsync();

        var lichHenText = ngayHenChot.ToString("dd/MM/yyyy HH:mm 'giờ'");
        var diaDiemText = string.IsNullOrWhiteSpace(don.DiaDiemGap) ? showroom.TenChiNhanh + " (" + showroom.DiaChi + ")" : don.DiaDiemGap;
        await _notif.SendAsync(don.MaKhachHang!, "Đơn cọc đã được tiếp nhận - Lịch hẹn của bạn",
            $"Đơn cọc #{don.MaDonCoc} đã được xác nhận. Lịch hẹn gặp: {lichHenText} tại: {diaDiemText}. Vui lòng đến đúng giờ để hoàn tất thủ tục.",
            "/Orders/DepositResult?maDonCoc=" + don.MaDonCoc);
        await _notif.SendToRoleAsync("Admin", "Đơn cọc đã được Quản Lý tiếp nhận",
            $"Đơn cọc #{don.MaDonCoc} - {don.HoTen} - Vui lòng vào bán xe",
            $"/Admin/DonCoc/Index");

        await _log.LogAsync($"QL tiếp nhận đơn cọc #{maDonCoc} - hẹn {lichHenText}");
        SuccessMessage = "Đã tiếp nhận đơn cọc và gửi lịch hẹn cho khách.";
        return RedirectToPage();
    }

    // ----- Quản Lý xác nhận ĐÃ NHẬN TIỀN cọc (đơn tiền mặt): chuyển sang "Chờ xác nhận" để tiếp tục luồng -----
    public async Task<IActionResult> OnPostConfirmCashReceivedAsync(int maDonCoc)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var don = await _db.DonDatCoc
            .Include(d => d.ChiTiets)
            .FirstOrDefaultAsync(d => d.MaDonCoc == maDonCoc);
        if (don == null) return NotFound();

        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null || don.MaChiNhanh != showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền xác nhận tiền cọc của showroom khác.";
            return RedirectToPage();
        }

        if (!string.Equals(don.PhuongThucThanhToan, "Tiền mặt", StringComparison.OrdinalIgnoreCase))
        {
            ErrorMessage = "Đơn này không phải thanh toán tiền mặt.";
            return RedirectToPage();
        }
        if (don.TrangThaiThanhToan == "Đã thanh toán")
        {
            ErrorMessage = "Đơn này đã được xác nhận nhận tiền cọc rồi.";
            return RedirectToPage();
        }

        don.TrangThaiThanhToan = "Đã thanh toán";
        // Sau khi đã thu tiền cọc, đơn chuyển về trạng thái để Quản Lý tiếp tục "Tiếp nhận đơn cọc"
        if (don.TrangThaiDonHang == "Chờ thanh toán") don.TrangThaiDonHang = "Chờ xác nhận";
        don.MaQuanLyDuyet = userName;
        await _db.SaveChangesAsync();

        await _notif.SendAsync(don.MaKhachHang!, "Đã nhận tiền cọc",
            $"Showroom đã xác nhận nhận tiền cọc đơn #{don.MaDonCoc}. Cảm ơn bạn đã đặt cọc!",
            "/Orders/DepositResult?maDonCoc=" + don.MaDonCoc);
        await _notif.SendToRoleAsync("Admin", "Đã nhận tiền cọc đơn tiền mặt",
            $"Quản Lý đã xác nhận nhận tiền cọc đơn #{don.MaDonCoc} - {don.HoTen}.",
            $"/Admin/DonCoc/Index");

        await _log.LogAsync($"QL xác nhận đã nhận tiền cọc đơn #{maDonCoc}");
        SuccessMessage = "Đã xác nhận nhận tiền cọc. Đơn chuyển sang trạng thái chờ tiếp nhận.";
        return RedirectToPage();
    }

    // ----- Huỷ đặt cọc (kèm lý do) - Quản Lý -----
    // Đơn bị huỷ sẽ NHẢ số lượng cọc (không tính vào "đã đặt cọc" trên giao diện), và gửi thông báo
    // cho khách kèm lý do người quản lý đã nhập (VD: hết hàng, khách hủy...).
    public async Task<IActionResult> OnPostCancelDepositOrderAsync(int maDonCoc, string? lyDoHuY)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        if (string.IsNullOrWhiteSpace(lyDoHuY))
        {
            ErrorMessage = "Vui lòng nhập lý do hủy đặt cọc.";
            return RedirectToPage();
        }

        var don = await _db.DonDatCoc
            .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(d => d.KhachHang)
            .FirstOrDefaultAsync(d => d.MaDonCoc == maDonCoc);
        if (don == null) return NotFound();

        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null || don.MaChiNhanh != showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền hủy đặt cọc của showroom khác.";
            return RedirectToPage();
        }

        if (don.TrangThaiDonHang == "Hoàn tất" || don.TrangThaiDonHang == "Đã hủy" || don.TrangThaiDonHang == "Đã từ chối")
        {
            ErrorMessage = "Đơn cọc này đã hoàn tất / đã hủy, không thể hủy thêm.";
            return RedirectToPage();
        }

        var lyDo = lyDoHuY.Trim();
        foreach (var ct in don.ChiTiets)
        {
            // Hoàn trả tồn kho cho xe đã "Đã tiếp nhận" (đã bị trừ hẳn khi tiếp nhận) khi hủy đơn
            if (ct.TrangThaiTiepNhan == "Đã tiếp nhận")
            {
                await CongTruTonKhoAsync(ct.MaPhienBan, ct.MaChiNhanh ?? "", +ct.SoLuong);
                if (ct.PhienBan != null)
                {
                    ct.PhienBan.SoLuongTrongKho += ct.SoLuong;
                }
            }
            ct.TrangThaiTiepNhan = "Từ chối";
            ct.LyDoTuChoi = lyDo;
            ct.NguoiPhanHoi = userName;
            ct.NgayPhanHoi = DateTime.Now;
        }
        don.TrangThaiDonHang = "Đã hủy";
        await _db.SaveChangesAsync(); // trạng thái "Đã hủy" không nằm trong bộ đếm đã đặt cọc → số cọc KHÔNG tăng

        await _log.LogAsync($"QL hủy đặt cọc #{maDonCoc}",
            $"Showroom {showroom.TenChiNhanh} - Lý do: {lyDo}");

        await _notif.SendToRoleAsync("Admin", "Quản Lý hủy đơn đặt cọc",
            $"Quản Lý đã hủy đơn cọc #{maDonCoc} - {don.HoTen}. Lý do: {lyDo}",
            $"/Admin/DonCoc/Index");

        if (don.MaKhachHang != null)
        {
            await _notif.SendAsync(don.MaKhachHang, "Đơn đặt cọc của bạn đã bị hủy",
                $"Đơn cọc #{maDonCoc} tại {showroom.TenChiNhanh} đã bị hủy. Lý do: {lyDo}. Tiền cọc sẽ được hoàn lại nếu đã thanh toán.\n(Chủ showroom gửi phản hồi)",
                "/Orders/DepositResult?maDonCoc=" + maDonCoc);
        }

        SuccessMessage = "Đã hủy đơn đặt cọc và gửi thông báo kèm lý do cho khách.";
        return RedirectToPage();
    }

    // ----- Bán thành công: tạo hoá đơn (cơ sở + tên khách + tiền), trừ kho, cập nhật doanh thu -----
    public async Task<IActionResult> OnPostSoldAsync(int maDonCoc)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var don = await _db.DonDatCoc
            .Include(d => d.PhienBan).ThenInclude(p => p.DongXe)
            .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .FirstOrDefaultAsync(d => d.MaDonCoc == maDonCoc);
        if (don == null) return NotFound();
        if (!don.ChiTiets.Any() && don.PhienBan == null) return NotFound();

        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null || don.MaChiNhanh != showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền bán đơn cọc của showroom khác.";
            return RedirectToPage();
        }

        if (don.TrangThaiDonHang != "Đã xác nhận")
        {
            ErrorMessage = "Chỉ đơn cọc đã tiếp nhận (Đã xác nhận) mới được bán.";
            return RedirectToPage();
        }

        var (hoaDon, tenXe) = await _sale.SellAsync(don, userName);

        await _notif.SendAsync(don.MaKhachHang!, "Mua xe thành công",
            $"Xe {tenXe} - Đơn cọc #{maDonCoc} đã hoàn tất. Hóa đơn {hoaDon.MaHoaDon} được xuất tại {showroom.TenChiNhanh}. Cảm ơn bạn đã mua xe!",
            $"/Orders/DepositResult?maDonCoc={maDonCoc}");
        await _notif.SendToRoleAsync("Admin", "Quản Lý đã bán xe thành công",
            $"Đơn cọc #{maDonCoc} - {don.HoTen} - xe {tenXe} đã bán tại {showroom.TenChiNhanh}. Hóa đơn {hoaDon.MaHoaDon}.",
            "/Admin/HoaDon");

        await _log.LogAsync($"QL bán thành công đơn cọc #{maDonCoc} - {tenXe} (HĐ {hoaDon.MaHoaDon})");
        SuccessMessage = $"Đã bán thành công xe {tenXe}. Hóa đơn {hoaDon.MaHoaDon} đã được tạo.";
        return RedirectToPage();
    }

    // ----- Không bán / Huỷ giao dịch (Quản Lý) -----
    public async Task<IActionResult> OnPostCancelSaleAsync(int maDonCoc)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var don = await _db.DonDatCoc
            .Include(d => d.PhienBan)
            .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan)
            .FirstOrDefaultAsync(d => d.MaDonCoc == maDonCoc);
        if (don == null) return NotFound();

        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null || don.MaChiNhanh != showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền hủy giao dịch của showroom khác.";
            return RedirectToPage();
        }

        if (don.TrangThaiDonHang != "Đã xác nhận" && don.TrangThaiDonHang != "Hoàn tất")
        {
            ErrorMessage = "Chỉ đơn đã tiếp nhận (Đã xác nhận) mới có thể hủy bán.";
            return RedirectToPage();
        }

        // Nếu đơn đã bán (Hoàn tất): xoá hoá đơn + hoàn doanh thu + trả lại xe
        var hoaDon = await _db.HoaDonMuaXe.FirstOrDefaultAsync(h => h.MaDonCoc == maDonCoc);
        if (hoaDon != null)
        {
            await HoanTacDoanhThu(don.MaChiNhanh ?? "", hoaDon.TongTienPhaiTra);
            _db.HoaDonMuaXe.Remove(hoaDon);

            var listPhienBan = don.ChiTiets.Any()
                ? don.ChiTiets.Select(c => c.PhienBan).Where(p => p != null).ToList()
                : new List<PhienBanXe?> { don.PhienBan };
            foreach (var p in listPhienBan)
            {
                if (p != null) p.SoLuongTrongKho++;
            }
        }

        don.TrangThaiDonHang = "Đã hủy";
        await _revenue.RevertDepositRevenueAsync(maDonCoc);
        await _db.SaveChangesAsync();

        await _notif.SendAsync(don.MaKhachHang!, "Giao dịch bị hủy",
            $"Giao dịch đơn cọc #{maDonCoc} tại {showroom.TenChiNhanh} đã bị hủy bán. Tiền cọc sẽ được hoàn lại.", "");
        await _notif.SendToRoleAsync("Admin", "Quản Lý hủy bán giao dịch",
            $"Đơn cọc #{maDonCoc} - {don.HoTen} đã bị Quản Lý hủy bán tại {showroom.TenChiNhanh}.", "/Admin/DonCoc");

        await _log.LogAsync($"QL hủy bán (Không bán) đơn cọc #{maDonCoc}");
        SuccessMessage = "Đã hủy giao dịch và trả xe về kho.";
        return RedirectToPage();
    }

    // ----- Nhập hàng bổ sung cho xe còn thiếu của đơn (Quản Lý showroom nguồn) -----
    // Khi showroom nhận đơn thiếu xe (SoLuongThieu > 0), QL có thể chủ động nhập thêm xe
    // vào showroom của mình để đủ số lượng phục vụ đơn; nếu nhập đủ thì tự tiếp nhận xe đó.
    public async Task<IActionResult> OnPostNhapHangAsync(int[] maChiTiet, int[] soLuongNhap, string? ghiChu = null)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return RedirectToPage();
        }

        if (maChiTiet == null || maChiTiet.Length == 0)
        {
            ErrorMessage = "Không có xe nào được chọn để nhập hàng.";
            return RedirectToPage();
        }

        var soLuongNhapLut = new Dictionary<int, int>();
        var danhSachMaChiTiet = maChiTiet.ToList();
        var chiTietList = await _db.DonDatCocChiTiet
            .Where(c => danhSachMaChiTiet.Contains(c.MaChiTiet))
            .Include(c => c.PhienBan)
            .Include(c => c.DonDatCoc)
            .ToListAsync();

        for (int i = 0; i < maChiTiet.Length; i++)
        {
            if (soLuongNhap != null && i < soLuongNhap.Length && soLuongNhap[i] > 0)
                soLuongNhapLut[maChiTiet[i]] = soLuongNhap[i];
        }

        int tongNhap = 0;
        var danhSachXe = new List<string>();
        bool coLoi = false;

        foreach (var chiTiet in chiTietList)
        {
            if (chiTiet.MaChiNhanh != showroom.MaChiNhanh)
            {
                coLoi = true;
                continue;
            }
            if (chiTiet.TrangThaiTiepNhan != "Chờ xác nhận" || !chiTiet.CanNhapHang)
            {
                coLoi = true;
                continue;
            }

            int soLuongNhapCt = soLuongNhapLut.TryGetValue(chiTiet.MaChiTiet, out var sl) ? sl : 0;
            if (soLuongNhapCt <= 0) continue;

            var maChiNhanh = chiTiet.MaChiNhanh ?? "";

            // 1. Tăng tồn kho của showroom nguồn
            var tonKho = await _db.TonKhoTheoChiNhanh
                .FirstOrDefaultAsync(t => t.MaPhienBan == chiTiet.MaPhienBan && t.MaChiNhanh == maChiNhanh);
            if (tonKho == null)
            {
                tonKho = new TonKhoTheoChiNhanh
                {
                    MaPhienBan = chiTiet.MaPhienBan,
                    MaChiNhanh = maChiNhanh,
                    SoLuong = soLuongNhapCt,
                    NgayCapNhat = DateTime.Now
                };
                _db.TonKhoTheoChiNhanh.Add(tonKho);
            }
            else
            {
                tonKho.SoLuong += soLuongNhapCt;
                tonKho.NgayCapNhat = DateTime.Now;
            }

            // 2. Tăng tổng tồn kho của phiên bản
            if (chiTiet.PhienBan != null)
            {
                chiTiet.PhienBan.SoLuongTrongKho += soLuongNhapCt;
            }

            // 3. Ghi phiếu nhập hàng (giá mỗi xe = GiaNhap của phiên bản)
            var giaNhapMoiXe = chiTiet.PhienBan?.GiaNhap ?? 0;
            var tongTienNhap = giaNhapMoiXe * soLuongNhapCt;
            _db.PhieuNhapXe.Add(new PhieuNhapXe
            {
                MaDonCoc = chiTiet.MaDonCoc,
                MaPhienBan = chiTiet.MaPhienBan,
                MaChiNhanh = maChiNhanh,
                SoLuongNhap = soLuongNhapCt,
                SoTienNhapMoiXe = giaNhapMoiXe,
                TongSoTienNhap = tongTienNhap,
                NguoiNhap = userName,
                NgayNhap = DateTime.Now,
                GhiChu = ghiChu
            });

            // 3b. Trừ doanh thu theo tổng tiền nhập hàng (GiaNhap × số lượng)
            await GiamDoanhThuNhapHangAsync(maChiNhanh, tongTienNhap);

            // 4. Cập nhật chi tiết đơn: hết thiếu sau nhập thì tự tiếp nhận xe
            chiTiet.DaNhapKho = true;
            chiTiet.SoTienNhapMoiXe = giaNhapMoiXe;
            chiTiet.SoLuongThieu = Math.Max(0, chiTiet.SoLuongThieu - soLuongNhapCt);
            if (chiTiet.SoLuongThieu <= 0)
            {
                chiTiet.TrangThaiTiepNhan = "Đã tiếp nhận";
                chiTiet.NguoiPhanHoi = userName;
                chiTiet.NgayPhanHoi = DateTime.Now;

                // Tự tiếp nhận đủ hàng = xe đã cam kết cho khách → trừ hẳn tồn kho vật lý
                await CongTruTonKhoAsync(chiTiet.MaPhienBan, chiTiet.MaChiNhanh ?? "", -chiTiet.SoLuong);
                if (chiTiet.PhienBan != null)
                {
                    chiTiet.PhienBan.SoLuongTrongKho = Math.Max(0, chiTiet.PhienBan.SoLuongTrongKho - chiTiet.SoLuong);
                }
            }

            tongNhap += soLuongNhapCt;
            danhSachXe.Add($"{chiTiet.PhienBan?.DongXe?.TenDong ?? ""} {chiTiet.PhienBan?.TenPhienBan ?? ""}".Trim());
        }

        await _db.SaveChangesAsync();

        var tenXe = string.Join(", ", danhSachXe.Distinct());
        if (tongNhap > 0)
        {
            await _log.LogAsync($"QL nhập hàng bổ sung cho đơn",
                $"Nhập {tongNhap} xe ({tenXe}) vào {showroom.TenChiNhanh}");
            SuccessMessage = $"Đã nhập {tongNhap} xe vào {showroom.TenChiNhanh}. " +
                (coLoi ? "Một số xe còn thiếu chưa xử lý (trạng thái không hợp lệ)." : "");
        }
        else
        {
            ErrorMessage = "Không có xe hợp lệ nào được nhập.";
        }
        return RedirectToPage();
    }

    // ----- Từ chối toàn bộ đơn cọc (Quản Lý showroom nhận xe không đủ điều kiện đáp ứng) -----
    // Khi showroom nhận đơn k thể đủ xe (các showroom khác từ chối, chưa nhập được hàng...) thì QL
    // được phép từ chối toàn bộ đơn → đánh dấu hết chi tiết chưa phản hồi là "Từ chối" và báo Admin.
    public async Task<IActionResult> OnPostDeclineOrderAsync(int maDonCoc, string? lyDoTuChoi)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        if (string.IsNullOrWhiteSpace(lyDoTuChoi))
        {
            ErrorMessage = "Vui lòng nhập lý do từ chối đơn cọc.";
            return RedirectToPage();
        }

        var don = await _db.DonDatCoc
            .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(d => d.KhachHang)
            .FirstOrDefaultAsync(d => d.MaDonCoc == maDonCoc);
        if (don == null) return NotFound();

        var showroom = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroom == null || don.MaChiNhanh != showroom.MaChiNhanh)
        {
            ErrorMessage = "Bạn không có quyền từ chối đơn cọc của showroom khác.";
            return RedirectToPage();
        }

        if (don.TrangThaiDonHang != "Chờ xác nhận" && don.TrangThaiDonHang != "Chờ xử lý")
        {
            ErrorMessage = "Đơn cọc không ở trạng thái có thể từ chối.";
            return RedirectToPage();
        }

        var lyDo = lyDoTuChoi.Trim();
        foreach (var ct in don.ChiTiets)
        {
            // Xe đã "Đã tiếp nhận" từng bị trừ HẲN tồn kho vật lý + tổng tồn kho phiên bản khi tiếp nhận.
            // Khi từ chối toàn bộ đơn, phải hoàn trả lại số xe đó cho showroom đã tiếp nhận.
            if (ct.TrangThaiTiepNhan == "Đã tiếp nhận")
            {
                await CongTruTonKhoAsync(ct.MaPhienBan, ct.MaChiNhanh ?? "", +ct.SoLuong);
                if (ct.PhienBan != null)
                {
                    ct.PhienBan.SoLuongTrongKho += ct.SoLuong;
                }
            }
            ct.TrangThaiTiepNhan = "Từ chối";
            ct.LyDoTuChoi = lyDo;
            ct.NguoiPhanHoi = userName;
            ct.NgayPhanHoi = DateTime.Now;
        }
        don.TrangThaiDonHang = "Đã từ chối";
        await _db.SaveChangesAsync();

        await _log.LogAsync($"QL từ chối toàn bộ đơn cọc #{maDonCoc}",
            $"Showroom {showroom.TenChiNhanh} - Lý do: {lyDo}");

        await _notif.SendToRoleAsync("Admin", "Showroom từ chối toàn bộ đơn cọc",
            $"Showroom {showroom.TenChiNhanh} từ chối nhận đơn cọc #{maDonCoc} - {don.HoTen}. Lý do: {lyDo}",
            $"/Admin/DonCoc/Edit/{maDonCoc}");

        if (don.MaKhachHang != null)
        {
            await _notif.SendAsync(don.MaKhachHang, "Đơn cọc bị từ chối toàn bộ",
                $"Đơn cọc #{maDonCoc} bị showroom {showroom.TenChiNhanh} từ chối. Lý do: {lyDo}. Chúng tôi sẽ liên hệ để hỗ trợ bạn.",
                "/Profile");
        }

        SuccessMessage = $"Đã từ chối toàn bộ đơn #{maDonCoc}. Lý do đã được gửi tới Admin và khách hàng.";
        return RedirectToPage();
    }

    // ----- Tạo yêu cầu điều chuyển xe TỪ showroom khác (Quản Lý showroom đích) -----
    public async Task<IActionResult> OnPostGuiYeuCauDieuChuyenAsync(int[] maChiTiet, string[] maChiNhanhNguon, int[] soLuongDieuChuyen)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var showroomDich = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroomDich == null)
        {
            ErrorMessage = "Bạn chưa được phân công quản lý showroom nào.";
            return RedirectToPage();
        }

        if (maChiTiet == null || maChiTiet.Length == 0)
        {
            ErrorMessage = "Không có xe nào được chọn để điều chuyển.";
            return RedirectToPage();
        }

        var soLuongLut = new Dictionary<int, int>();
        var nguonLut = new Dictionary<int, string>();
        for (int i = 0; i < maChiTiet.Length; i++)
        {
            if (soLuongDieuChuyen != null && i < soLuongDieuChuyen.Length && soLuongDieuChuyen[i] > 0)
                soLuongLut[maChiTiet[i]] = soLuongDieuChuyen[i];
            if (maChiNhanhNguon != null && i < maChiNhanhNguon.Length && !string.IsNullOrWhiteSpace(maChiNhanhNguon[i]))
                nguonLut[maChiTiet[i]] = maChiNhanhNguon[i];
        }

        var chiTietList = await _db.DonDatCocChiTiet
            .Where(c => maChiTiet.ToList().Contains(c.MaChiTiet))
            .Include(c => c.DonDatCoc)
            .Include(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .Include(c => c.ChiNhanh)
            .ToListAsync();

        int tongYeuCau = 0;
        var danhSachXe = new List<string>();
        bool coLoi = false;

        foreach (var chiTiet in chiTietList)
        {
            int soLuong = soLuongLut.TryGetValue(chiTiet.MaChiTiet, out var sl) ? sl : 0;
            string maNguon = nguonLut.TryGetValue(chiTiet.MaChiTiet, out var mn) ? mn : "";
            if (soLuong <= 0 || string.IsNullOrEmpty(maNguon)) continue;

            if (chiTiet.MaChiNhanh != showroomDich.MaChiNhanh)
            {
                coLoi = true;
                continue;
            }
            if (!chiTiet.CanNhapHang || chiTiet.TrangThaiTiepNhan != "Chờ xác nhận")
            {
                coLoi = true;
                continue;
            }
            if (maNguon == showroomDich.MaChiNhanh)
            {
                coLoi = true;
                continue;
            }

            var availNguon = await TonKhoConLaiCuaShowroomAsync(chiTiet.MaPhienBan, maNguon, excludeMaChiTiet: chiTiet.MaChiTiet);
            if (availNguon < soLuong)
            {
                coLoi = true;
                continue;
            }

            var nguon = await _db.ChiNhanhShowroom.FindAsync(maNguon);
            var yc = new YeuCauDieuChuyenXe
            {
                MaDonCoc = chiTiet.MaDonCoc,
                MaChiTiet = chiTiet.MaChiTiet,
                MaPhienBan = chiTiet.MaPhienBan,
                MaChiNhanhNguon = maNguon,
                MaChiNhanhDich = chiTiet.MaChiNhanh ?? "",
                SoLuong = soLuong,
                TrangThai = "Chờ xác nhận",
                NguoiGui = userName,
                NgayGui = DateTime.Now
            };
            _db.YeuCauDieuChuyenXe.Add(yc);
            await _db.SaveChangesAsync();

            var tenXe = PrettyTenXe(chiTiet);
            if (!string.IsNullOrEmpty(nguon?.MaQuanLy))
            {
                await _notif.SendAsync(nguon!.MaQuanLy!, "Yêu cầu điều chuyển xe",
                    $"Showroom {showroomDich.TenChiNhanh} cần điều chuyển {soLuong} xe {tenXe} (Đơn cọc #{chiTiet.MaDonCoc}). Vui lòng Xác nhận hoặc Từ chối.",
                    $"/QuanLy/DonCoc?highlightYc={yc.MaYeuCau}");
            }

            await _log.LogAsync($"QL gửi yêu cầu điều chuyển #{yc.MaYeuCau}",
                $"{showroomDich.TenChiNhanh} <- {nguon?.TenChiNhanh} - {soLuong} xe {tenXe}");
            tongYeuCau += soLuong;
            danhSachXe.Add(tenXe);
        }

        if (tongYeuCau > 0)
        {
            SuccessMessage = $"Đã gửi {tongYeuCau} yêu cầu điều chuyển ({string.Join(", ", danhSachXe.Distinct())}) và đang chờ showroom nguồn xác nhận." +
                (coLoi ? " Một số xe không hợp lệ đã bỏ qua." : "");
        }
        else
        {
            ErrorMessage = "Không có xe hợp lệ nào được gửi điều chuyển (thiếu showroom nguồn hoặc showroom nguồn không đủ xe).";
        }
        return RedirectToPage();
    }

    // ----- Showroom nguồn XÁC NHẬN điều chuyển: trừ xe nguồn, cộng xe đích, cập nhật tồn kho 2 bên -----
    public async Task<IActionResult> OnPostXacNhanDieuChuyenAsync(int maYeuCau)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var yc = await _db.YeuCauDieuChuyenXe
            .Include(y => y.ChiTiet).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .FirstOrDefaultAsync(y => y.MaYeuCau == maYeuCau);
        if (yc == null) return NotFound();

        var showroomNguon = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroomNguon == null || yc.MaChiNhanhNguon != showroomNguon.MaChiNhanh)
        {
            ErrorMessage = "Bạn không phải quản lý của showroom nguồn để xác nhận điều chuyển này.";
            return RedirectToPage();
        }
        if (yc.TrangThai != "Chờ xác nhận")
        {
            ErrorMessage = "Yêu cầu điều chuyển này đã được xử lý.";
            return RedirectToPage();
        }

        var availNguon = await TonKhoConLaiCuaShowroomAsync(yc.MaPhienBan, yc.MaChiNhanhNguon, excludeMaChiTiet: yc.MaChiTiet);
        if (availNguon < yc.SoLuong)
        {
            yc.TrangThai = "Đã từ chối";
            yc.NguoiPhanHoi = userName;
            yc.NgayPhanHoi = DateTime.Now;
            yc.LyDoTuChoi = $"Hết xe thực tế (chỉ còn {Math.Max(0, availNguon)} xe)";
            await _db.SaveChangesAsync();
            ErrorMessage = "Showroom nguồn hiện không đủ xe để điều chuyển. Yêu cầu đã bị từ chối tự động.";
            return RedirectToPage();
        }

        await CongTruTonKhoAsync(yc.MaPhienBan, yc.MaChiNhanhNguon, -yc.SoLuong);
        await CongTruTonKhoAsync(yc.MaPhienBan, yc.MaChiNhanhDich, +yc.SoLuong);

        yc.TrangThai = "Đã xác nhận";
        yc.NguoiPhanHoi = userName;
        yc.NgayPhanHoi = DateTime.Now;

        if (yc.ChiTiet != null)
        {
            yc.ChiTiet.SoLuongThieu = Math.Max(0, yc.ChiTiet.SoLuongThieu - yc.SoLuong);
            if (yc.ChiTiet.SoLuongThieu <= 0)
            {
                yc.ChiTiet.TrangThaiTiepNhan = "Đã tiếp nhận";
                yc.ChiTiet.DaNhapKho = false;
                yc.ChiTiet.CanNhapHang = false;
                yc.ChiTiet.NguoiPhanHoi = userName;
                yc.ChiTiet.NgayPhanHoi = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();

        var tenXe = yc.ChiTiet != null ? PrettyTenXe(yc.ChiTiet) : "";
        var nguon = await _db.ChiNhanhShowroom.FindAsync(yc.MaChiNhanhNguon);
        var dich = await _db.ChiNhanhShowroom.FindAsync(yc.MaChiNhanhDich);
        await _notif.SendToRoleAsync("Admin", "Đã điều chuyển xe",
            $"Showroom {nguon?.TenChiNhanh} đã điều chuyển {yc.SoLuong} xe {tenXe} tới {dich?.TenChiNhanh} (Đơn cọc #{yc.MaDonCoc}).",
            $"/Admin/DonCoc/Index");
        if (yc.ChiTiet?.DonDatCoc is DonDatCoc don && !string.IsNullOrEmpty(don.MaKhachHang))
        {
            await _notif.SendAsync(don.MaKhachHang!, "Xe đang được điều chuyển tới showroom của bạn",
                $"Xe {tenXe} (Đơn cọc #{don.MaDonCoc}) đang được điều chuyển tới {dich?.TenChiNhanh}. {dich?.TenChiNhanh} sẽ sớm tiếp nhận đơn của bạn.",
                $"/Orders/DepositResult?maDonCoc={yc.MaDonCoc}");
        }

        await _log.LogAsync($"QL xác nhận điều chuyển #{maYeuCau}",
            $"{nguon?.TenChiNhanh} -> {dich?.TenChiNhanh} - {yc.SoLuong} xe {tenXe}");
        SuccessMessage = $"Đã xác nhận điều chuyển {yc.SoLuong} xe tới {dich?.TenChiNhanh}.";
        return RedirectToPage();
    }

    // ----- Showroom nguồn TỪ CHỐI điều chuyển (không đổi tồn kho) -----
    public async Task<IActionResult> OnPostTuChoiDieuChuyenAsync(int maYeuCau, string? lyDoTuChoi)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");
        if (string.IsNullOrWhiteSpace(lyDoTuChoi))
        {
            ErrorMessage = "Vui lòng nhập lý do từ chối điều chuyển.";
            return RedirectToPage();
        }

        var yc = await _db.YeuCauDieuChuyenXe
            .Include(y => y.DonDatCoc)
            .Include(y => y.ChiTiet).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .FirstOrDefaultAsync(y => y.MaYeuCau == maYeuCau);
        if (yc == null) return NotFound();

        var showroomNguon = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroomNguon == null || yc.MaChiNhanhNguon != showroomNguon.MaChiNhanh)
        {
            ErrorMessage = "Bạn không phải quản lý của showroom nguồn để từ chối điều chuyển này.";
            return RedirectToPage();
        }
        if (yc.TrangThai != "Chờ xác nhận")
        {
            ErrorMessage = "Yêu cầu điều chuyển này đã được xử lý.";
            return RedirectToPage();
        }

        yc.TrangThai = "Đã từ chối";
        yc.NguoiPhanHoi = userName;
        yc.NgayPhanHoi = DateTime.Now;
        yc.LyDoTuChoi = lyDoTuChoi.Trim();
        await _db.SaveChangesAsync();

        var nguon = await _db.ChiNhanhShowroom.FindAsync(yc.MaChiNhanhNguon);
        var dich = await _db.ChiNhanhShowroom.FindAsync(yc.MaChiNhanhDich);
        var tenXe = yc.ChiTiet != null ? PrettyTenXe(yc.ChiTiet) : "";
        if (!string.IsNullOrEmpty(yc.NguoiGui))
        {
            await _notif.SendAsync(yc.NguoiGui, "Yêu cầu điều chuyển bị từ chối",
                $"Showroom {nguon?.TenChiNhanh} đã từ chối điều chuyển {yc.SoLuong} xe {tenXe} cho {dich?.TenChiNhanh} (Đơn cọc #{yc.MaDonCoc}). Lý do: {lyDoTuChoi.Trim()}. Bạn có thể chọn showroom khác để điều chuyển.",
                $"/QuanLy/DonCoc?highlight={yc.MaDonCoc}");
        }

        await _log.LogAsync($"QL từ chối điều chuyển #{maYeuCau}", $"Lý do: {lyDoTuChoi.Trim()}");
        SuccessMessage = $"Đã từ chối yêu cầu điều chuyển #{maYeuCau}.";
        return RedirectToPage();
    }

    // ----- Showroom đích TỰ HUỶ yêu cầu điều chuyển đã gửi đi (chưa được xác nhận) -----
    public async Task<IActionResult> OnPostHuyYeuCauAsync(int maYeuCau)
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName)) return RedirectToPage("/Account/Login");

        var yc = await _db.YeuCauDieuChuyenXe
            .Include(y => y.DonDatCoc)
            .Include(y => y.ChiTiet).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
            .FirstOrDefaultAsync(y => y.MaYeuCau == maYeuCau);
        if (yc == null) return NotFound();

        var showroomDich = await _db.ChiNhanhShowroom.FirstOrDefaultAsync(c => c.MaQuanLy == userName);
        if (showroomDich == null || yc.MaChiNhanhDich != showroomDich.MaChiNhanh)
        {
            ErrorMessage = "Bạn không phải quản lý của showroom đích để huỷ yêu cầu này.";
            return RedirectToPage();
        }
        if (yc.TrangThai != "Chờ xác nhận")
        {
            ErrorMessage = "Yêu cầu điều chuyển này đã được xử lý, không thể huỷ.";
            return RedirectToPage();
        }

        yc.TrangThai = "Đã hủy";
        yc.NguoiPhanHoi = userName;
        yc.NgayPhanHoi = DateTime.Now;
        yc.LyDoTuChoi = "Huỷ bởi showroom đích";
        await _db.SaveChangesAsync();

        var nguon = await _db.ChiNhanhShowroom.FindAsync(yc.MaChiNhanhNguon);
        await _log.LogAsync($"QL showroom đích huỷ yêu cầu điều chuyển #{maYeuCau}",
            $"{showroomDich.TenChiNhanh} huỷ yêu cầu gửi tới {nguon?.TenChiNhanh} ({yc.SoLuong} xe)");
        SuccessMessage = $"Đã huỷ yêu cầu điều chuyển #{maYeuCau}.";
        return RedirectToPage();
    }

    // ---------- Helpers ----------
    private static string PrettyTenXe(DonDatCocChiTiet c)
        => $"{c.PhienBan?.DongXe?.TenDong ?? ""} {c.PhienBan?.TenPhienBan ?? ""}".Trim();

    // Tồn kho thực tế có thể dùng của 1 showroom cho 1 phiên bản = tồn kho - xe đã giữ cho đơn đã thanh toán (chưa huỷ/hoàn tất)
    private async Task<int> TonKhoConLaiCuaShowroomAsync(int maPhienBan, string maChiNhanh, int? excludeMaChiTiet = null)
    {
        var tonKho = await _db.TonKhoTheoChiNhanh
            .FirstOrDefaultAsync(t => t.MaPhienBan == maPhienBan && t.MaChiNhanh == maChiNhanh);
        var held = await _db.DonDatCocChiTiet
            .Where(c => c.MaPhienBan == maPhienBan && c.MaChiNhanh == maChiNhanh
                && (excludeMaChiTiet == null || c.MaChiTiet != excludeMaChiTiet)
                && c.DonDatCoc != null
                && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã từ chối"
                && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất"
                // Xe đã tiếp nhận sẽ được trừ hẳn khỏi tồn kho vật lý ở handler Tiếp nhận,
                // nên không đếm vào "đang giữ" nữa (tránh trừ 2 lần).
                && c.TrangThaiTiepNhan != "Đã tiếp nhận")
            .SumAsync(c => (int?)c.SoLuong) ?? 0;
        return Math.Max(0, (tonKho?.SoLuong ?? 0) - held);
    }

    private async Task CongTruTonKhoAsync(int maPhienBan, string maChiNhanh, int delta)
    {
        if (string.IsNullOrEmpty(maChiNhanh) || delta == 0) return;
        var tonKho = await _db.TonKhoTheoChiNhanh
            .FirstOrDefaultAsync(t => t.MaPhienBan == maPhienBan && t.MaChiNhanh == maChiNhanh);
        if (tonKho == null)
        {
            if (delta < 0) return;
            tonKho = new TonKhoTheoChiNhanh
            {
                MaPhienBan = maPhienBan,
                MaChiNhanh = maChiNhanh,
                SoLuong = delta,
                NgayCapNhat = DateTime.Now
            };
            _db.TonKhoTheoChiNhanh.Add(tonKho);
        }
        else
        {
            tonKho.SoLuong = Math.Max(0, tonKho.SoLuong + delta);
            tonKho.NgayCapNhat = DateTime.Now;
        }
    }

    private async Task GiamDoanhThuNhapHangAsync(string maChiNhanh, long soTienNhap)
    {
        if (string.IsNullOrEmpty(maChiNhanh) || soTienNhap <= 0) return;

        var kyBaoCao = DateTime.Now.ToString("yyyy-MM");
        var thongKe = await _db.ThongKeTongHop_Boss
            .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == maChiNhanh);

        if (thongKe == null)
        {
            thongKe = new ThongKeTongHop_Boss
            {
                KyBaoCao = kyBaoCao,
                MaChiNhanh = maChiNhanh,
                TongDoanhThu = 0,
                TongTienCocThuVe = 0,
                TongSoXeDaBan = 0,
                SoDonCocBiHuy = 0,
                TongLuotXemWeb = 0,
                TongLuotLaiThu = 0,
                MaDongXeBanChayNhat = 0
            };
            _db.ThongKeTongHop_Boss.Add(thongKe);
        }

        thongKe.TongDoanhThu = Math.Max(0, thongKe.TongDoanhThu - soTienNhap);
    }

    private async Task HoanTacDoanhThu(string maChiNhanh, long soTien)
    {
        var kyBaoCao = DateTime.Now.ToString("yyyy-MM");
        var thongKe = await _db.ThongKeTongHop_Boss
            .FirstOrDefaultAsync(t => t.KyBaoCao == kyBaoCao && t.MaChiNhanh == maChiNhanh);
        if (thongKe != null)
        {
            thongKe.TongDoanhThu -= soTien;
            thongKe.TongSoXeDaBan--;
            if (thongKe.TongSoXeDaBan < 0) thongKe.TongSoXeDaBan = 0;
            if (thongKe.TongDoanhThu < 0) thongKe.TongDoanhThu = 0;
        }
    }
}
