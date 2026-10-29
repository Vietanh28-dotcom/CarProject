using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CarProject.Pages.Orders;

[Authorize]
public class DepositFormModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;
    private readonly INotificationService _notif;
    private readonly SepaySettings _sepay;

    public DongXe Dong { get; set; }
    public List<PhienBanOption> PhienBans { get; set; } = new();
    public List<ChiNhanhShowroom> DanhSachChiNhanh { get; set; } = new();
    public string SuccessMessage { get; set; }
    public decimal TotalDeposit { get; set; }
    public int TongXe { get; set; }

    private static string BuildDiaDiemNote(string diaDiem, string? toaDo)
    {
        var note = diaDiem;
        if (!string.IsNullOrWhiteSpace(toaDo))
        {
            var link = "https://www.google.com/maps?q=" + Uri.EscapeDataString(toaDo);
            note += $" (Tọa độ: {toaDo}\nLink Google Maps: {link})";
        }
        return note;
    }

    public DateTime NgayHenToiThieu => DateTime.Today.AddDays(4);
    public DateTime NgayHenToiDa => DateTime.Today.AddMonths(1).AddDays(-1);

    [BindProperty]
    public string HoTen { get; set; } = "";
    [BindProperty]
    public string SoDienThoai { get; set; } = "";
    [BindProperty]
    public string DiaChi { get; set; } = "";
    [BindProperty]
    public string PhuongThucThanhToan { get; set; } = "";
    [BindProperty]
    public string? GhiChu { get; set; }
    [BindProperty]
    public string MaChiNhanh { get; set; } = "";

    [BindProperty]
    public string? DiaDiemGap { get; set; }

    [BindProperty]
    public string? ToaDoGap { get; set; }

    [BindProperty]
    public int SoLuongPhienBan { get; set; } = 1;

    [BindProperty]
    public Dictionary<int, int> SoLuongByPhienBan { get; set; } = new();

    [BindProperty(Name = "_ajax")]
    public bool IsAjaxSubmit { get; set; }

    private Dictionary<(int, string), int> _tonKhoLut = new();

    public DepositFormModel(AppDbContext db, IActivityLogService log, INotificationService notif,
        IOptions<SepaySettings> sepay)
    {
        _db = db;
        _log = log;
        _notif = notif;
        _sepay = sepay.Value;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        await LoadDongAsync(id);
        if (Dong == null)
            return NotFound();

        await LoadShowroomsAsync();
        await _log.LogAsync("Xem form đặt cọc", $"{Dong.TenDong} (ID={id})");
        return Page();
    }

    private async Task LoadDongAsync(int id)
    {
        Dong = await _db.DongXe
            .Include(d => d.HangXe)
            .Include(d => d.PhienBanXes)
            .FirstOrDefaultAsync(d => d.MaDong == id);
        if (Dong == null) return;

        var phienBans = Dong.PhienBanXes.ToList();
        var maPbs = phienBans.Select(p => p.MaPhienBan).ToList();

        // Số xe đã đặt cọc (chỉ đơn ĐÃ THANH TOÁN giữ xe)
        var daDatCoc = await _db.DonDatCocChiTiet
            .Where(c => maPbs.Contains(c.MaPhienBan)
                && c.DonDatCoc != null
                && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán")
            .GroupBy(c => c.MaPhienBan)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.SoLuong));

        PhienBans = phienBans.Select(p =>
        {
            var reserved = daDatCoc.TryGetValue(p.MaPhienBan, out var r) ? r : 0;
            return new PhienBanOption
            {
                MaPhienBan = p.MaPhienBan,
                TenPhienBan = p.TenPhienBan,
                GiaNiemYet = p.GiaNiemYet,
                SoLuongTrongKho = p.SoLuongTrongKho,
                DaDatCoc = reserved,
                ConLai = Math.Max(0, p.SoLuongTrongKho),
                IsPreOrder = p.SoLuongTrongKho <= 0,
                MauSac = p.MauSac ?? "",
                DongCo = p.DongCo ?? "",
                HopSo = p.HopSo ?? ""
            };
        }).ToList();
    }

    private async Task LoadShowroomsAsync()
    {
        var all = await _db.ChiNhanhShowroom
            .Where(c => c.TrangThai == "Active" || c.TrangThai == "Hoạt động")
            .ToListAsync();

        // Hiển thị tất cả showroom đang hoạt động để khách có thể chọn showroom phù hợp.
        DanhSachChiNhanh = all;
    }

    private async Task<List<string>> GetOtherReservationsAsync(List<int> selectedPbIds, string currentUser)
    {
        var cartReserved = await _db.GioHang
            .Where(g => g.MaTaiKhoan != currentUser && selectedPbIds.Contains(g.MaPhienBan))
            .GroupBy(g => g.MaPhienBan)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.SoLuong));

        var depositReserved = await _db.DonDatCocChiTiet
            .Where(c => selectedPbIds.Contains(c.MaPhienBan)
                && c.DonDatCoc != null
                && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                && c.DonDatCoc!.TrangThaiDonHang != "Từ chối"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã từ chối"
                && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất")
            .GroupBy(c => c.MaPhienBan)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.SoLuong));

        var messages = new List<string>();
        foreach (var kv in selectedPbIds)
        {
            var cartQty = cartReserved.TryGetValue(kv, out var cart) ? cart : 0;
            var depositQty = depositReserved.TryGetValue(kv, out var dep) ? dep : 0;
            if (cartQty + depositQty > 0)
            {
                messages.Add($"Phiên bản {kv}: đã có {cartQty + depositQty} xe đang được giữ bởi người khác trong giỏ hàng hoặc đơn cọc.");
            }
        }

        return messages;
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        await LoadDongAsync(id);
        if (Dong == null)
            return NotFound();

        if (string.IsNullOrEmpty(HoTen) || string.IsNullOrEmpty(SoDienThoai))
        {
            if (IsAjaxSubmit) return JsonError("Vui lòng điền họ tên và số điện thoại.");
            ModelState.AddModelError("", "Vui lòng điền họ tên và số điện thoại.");
            await LoadShowroomsAsync();
            return Page();
        }

        if (string.IsNullOrEmpty(MaChiNhanh))
        {
            if (IsAjaxSubmit) return JsonError("Vui lòng chọn showroom nguồn.");
            ModelState.AddModelError("", "Vui lòng chọn showroom nguồn.");
            await LoadShowroomsAsync();
            return Page();
        }

        // Lọc các phiên bản người dùng chọn (số lượng > 0)
        var selected = SoLuongByPhienBan
            .Where(kv => kv.Key > 0 && kv.Value > 0)
            .ToList();

        if (selected.Count == 0)
        {
            if (IsAjaxSubmit) return JsonError("Vui lòng chọn ít nhất 1 phiên bản xe.");
            ModelState.AddModelError("", "Vui lòng chọn ít nhất 1 phiên bản xe.");
            await LoadShowroomsAsync();
            return Page();
        }

        var wantVersionCount = Math.Max(1, SoLuongPhienBan);
        if (selected.Count > wantVersionCount)
        {
            if (IsAjaxSubmit) return JsonError($"Bạn đã chọn {selected.Count} phiên bản nhưng tối đa chỉ được chọn {wantVersionCount} phiên bản.");
            ModelState.AddModelError("", $"Bạn đã chọn {selected.Count} phiên bản nhưng tối đa chỉ được chọn {wantVersionCount} phiên bản.");
            await LoadShowroomsAsync();
            return Page();
        }

        if (wantVersionCount == 1 && selected.Count != 1)
        {
            if (IsAjaxSubmit) return JsonError("Nếu chọn 1 phiên bản, vui lòng tích chọn đúng 1 phiên bản xe.");
            ModelState.AddModelError("", "Nếu chọn 1 phiên bản, vui lòng tích chọn đúng 1 phiên bản xe.");
            await LoadShowroomsAsync();
            return Page();
        }

        if (wantVersionCount >= 2 && selected.Count < 2)
        {
            if (IsAjaxSubmit) return JsonError("Nếu chọn từ 2 phiên bản trở lên, vui lòng chọn ít nhất 2 phiên bản khác nhau.");
            ModelState.AddModelError("", "Nếu chọn từ 2 phiên bản trở lên, vui lòng chọn ít nhất 2 phiên bản khác nhau.");
            await LoadShowroomsAsync();
            return Page();
        }

        var totalXe = selected.Sum(kv => kv.Value);
        var currentUser = User.GetJwtUserName() ?? "";
        var selectedPbIds = selected.Select(kv => kv.Key).ToList();
        var stockRows = await _db.TonKhoTheoChiNhanh
            .Where(t => selectedPbIds.Contains(t.MaPhienBan) && t.MaChiNhanh == MaChiNhanh)
            .ToListAsync();
        var stockByPb = stockRows
            .GroupBy(t => t.MaPhienBan)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SoLuong));

        foreach (var kv in selected)
        {
            var opt = PhienBans.FirstOrDefault(p => p.MaPhienBan == kv.Key);
            if (opt == null)
            {
                if (IsAjaxSubmit) return JsonError($"Phiên bản {kv.Key} không tồn tại.");
                ModelState.AddModelError("", $"Phiên bản {kv.Key} không tồn tại.");
                await LoadShowroomsAsync();
                return Page();
            }

            var stockInShowroom = stockByPb.TryGetValue(kv.Key, out var tk) ? tk : 0;
            var cartReserved = await _db.GioHang
                .Where(g => g.MaTaiKhoan != currentUser && g.MaPhienBan == kv.Key)
                .SumAsync(g => (int?)g.SoLuong) ?? 0;
            var depositReserved = await _db.DonDatCocChiTiet
                .Where(c => c.MaPhienBan == kv.Key
                    && c.DonDatCoc != null
                    && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                    && c.DonDatCoc!.TrangThaiDonHang != "Từ chối"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã từ chối"
                    && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất")
                .SumAsync(c => (int?)c.SoLuong) ?? 0;
            var available = Math.Max(0, stockInShowroom - cartReserved - depositReserved);

            // Phiên bản hết hàng (đặt trước) không bị ràng buộc tồn kho hiện tại
            if (!opt.IsPreOrder && kv.Value > available)
            {
                var msg = $"Phiên bản \"{opt.TenPhienBan}\" đã có người đặt cọc hoặc đang có người khác thêm vào giỏ hàng. Hiện chỉ còn {available} xe chưa bị đặt cọc ở showroom này.";
                if (IsAjaxSubmit) return JsonError(msg);
                ModelState.AddModelError("", msg);
                await LoadShowroomsAsync();
                return Page();
            }
        }

        // Ngày hẹn nhận xe do Quản lý chốt sau khi tiếp nhận đơn — user không tự chọn
        var depositRate = totalXe <= 2 ? DepositCalculator.PreOrderRate : DepositCalculator.InStockRate;
        TotalDeposit = selected.Sum(kv =>
        {
            var opt = PhienBans.First(p => p.MaPhienBan == kv.Key);
            return DepositCalculator.ComputeByQuantity(opt.GiaNiemYet, totalXe) * kv.Value;
        });
        TongXe = totalXe;
        var hasPreOrder = selected.Any(kv => PhienBans.First(p => p.MaPhienBan == kv.Key).IsPreOrder);
        var isCashPayment = string.Equals(PhuongThucThanhToan, "Tiền mặt", StringComparison.OrdinalIgnoreCase);

        if (isCashPayment)
        {
            if (string.IsNullOrWhiteSpace(DiaDiemGap))
            {
                var msgDiaDiem = "Vui lòng chọn địa điểm nhân viên quản lý đến thu tiền cọc trên bản đồ.";
                if (IsAjaxSubmit) return JsonError(msgDiaDiem);
                ModelState.AddModelError("", msgDiaDiem);
                await LoadShowroomsAsync();
                return Page();
            }
        }
        else
        {
            DiaDiemGap = null;
            ToaDoGap = null;
        }

        // ===== Transaction + khoá dòng để chống bán vượt tồn kho =====
        await using var tx = await _db.Database.BeginTransactionAsync();

        foreach (var kv in selected)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "SELECT MaTonKho FROM TonKhoTheoChiNhanh WITH (UPDLOCK, HOLDLOCK) WHERE MaPhienBan = {0}", kv.Key);
        }

        var selectedPbs = selected.Select(kv => kv.Key).ToList();
        var tonKhoRows = await _db.TonKhoTheoChiNhanh
            .Where(t => selectedPbs.Contains(t.MaPhienBan))
            .ToListAsync();
        _tonKhoLut = tonKhoRows
            .GroupBy(t => (t.MaPhienBan, t.MaChiNhanh))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.SoLuong));

        var maGiaoDich = $"DH{DateTime.Now:yyMMddHHmmss}{Dong.MaDong}";
        var deposit = new DonDatCoc
        {
            MaKhachHang = User.GetJwtUserName() ?? "",
            MaChiNhanh = MaChiNhanh,
            SoTienCoc = TotalDeposit,
            PhuongThucThanhToan = string.IsNullOrEmpty(PhuongThucThanhToan) ? "Chuyển khoản" : PhuongThucThanhToan,
            TrangThaiThanhToan = isCashPayment ? "Chờ thanh toán" : "Chưa thanh toán",
            TrangThaiDonHang = isCashPayment ? "Chờ thanh toán" : (hasPreOrder ? "Chờ xử lý" : "Chờ xác nhận"),
            NgayTaoDon = DateTime.Now,
            NgayHenNhanXe = null,
            HoTen = HoTen,
            SoDienThoai = SoDienThoai,
            DiaChi = DiaChi ?? "",
            GhiChu = GhiChu ?? "",
            DiaDiemGap = DiaDiemGap,
            ToaDoGap = ToaDoGap,
            MaGiaoDich = maGiaoDich
        };

        foreach (var kv in selected)
        {
            var opt = PhienBans.First(p => p.MaPhienBan == kv.Key);
            var isPreOrder = opt.IsPreOrder;
            var tonKhoCn = _tonKhoLut.TryGetValue((kv.Key, MaChiNhanh), out var tk) ? tk : 0;
            var soLuongThieu = isPreOrder ? kv.Value : Math.Max(0, kv.Value - tonKhoCn);

            deposit.ChiTiets.Add(new DonDatCocChiTiet
            {
                MaPhienBan = kv.Key,
                MaChiNhanh = MaChiNhanh,
                SoLuong = kv.Value,
                SoTienCocPhanBo = DepositCalculator.ComputeByQuantity(opt.GiaNiemYet, totalXe) * kv.Value,
                SoLuongThieu = soLuongThieu
            });
        }

        _db.DonDatCoc.Add(deposit);
        await _db.SaveChangesAsync();

var showroom = await _db.ChiNhanhShowroom.FindAsync(MaChiNhanh);
        var showroomName = showroom?.TenChiNhanh ?? MaChiNhanh;

        if (isCashPayment)
        {
            await _notif.SendToRoleAsync("Admin", "Đơn đặt cọc mới - chờ thanh toán",
                $"Khách {HoTen} đã đặt cọc {totalXe} xe bằng tiền mặt. Đơn #{deposit.MaDonCoc} đang chờ thanh toán tại {showroomName}.",
                $"/Admin/DonCoc/Index");
        }

        // Gửi toạ độ địa điểm hẹn cho quản lý showroom (chỉ với tiền mặt) để đến đúng chỗ thu tiền cọc
        if (isCashPayment)
        {
            var diaDiemGui = string.IsNullOrWhiteSpace(DiaDiemGap)
                ? "Chưa chọn địa điểm"
                : BuildDiaDiemNote(DiaDiemGap, ToaDoGap);
            var managerMaQuanLy = showroom?.MaQuanLy;
            if (!string.IsNullOrWhiteSpace(managerMaQuanLy))
            {
                var manager = await _db.TaiKhoan.FirstOrDefaultAsync(t => t.TenDangNhap == managerMaQuanLy);
                if (manager != null && !string.IsNullOrWhiteSpace(manager.TenDangNhap))
                {
                    await _notif.SendAsync(manager.TenDangNhap, "Đơn đặt cọc mới - địa điểm thu tiền cọc",
                        $"Khách {HoTen} đã đặt cọc {totalXe} xe. Vị trí đến thu tiền cọc: {diaDiemGui}. Vui lòng kiểm tra đơn #{deposit.MaDonCoc}.",
                        $"/QuanLy/DonCoc?highlight={deposit.MaDonCoc}");
                }
            }
        }

        // Tạo lịch hẹn nhận xe tại showroom nguồn
        var lichHen = new LichHenLaiThu
        {
            MaKhachHang = User.GetJwtUserName() ?? "",
            MaDong = Dong.MaDong,
            MaChiNhanh = MaChiNhanh,
            HoTenNguoiLai = HoTen,
            SoDienThoai = SoDienThoai,
            SoBangLaiXe = "",
            NgayHen = null,
            GioHen = null,
            TrangThai = "Chờ xác nhận",
            YKienKhachHang = GhiChu ?? ""
        };
        _db.LichHenLaiThu.Add(lichHen);

        await tx.CommitAsync();

        var listXe = string.Join(", ", selected.Select(kv =>
        {
            var opt = PhienBans.First(p => p.MaPhienBan == kv.Key);
            return $"{opt.TenPhienBan} x{kv.Value}";
        }));

        await _log.LogAsync("Gửi đơn đặt cọc",
            $"{HoTen} - {SoDienThoai} - {Dong.TenDong} ({listXe}) - {TotalDeposit:N0} VNĐ");

        var result = new
        {
            maDonCoc = deposit.MaDonCoc,
            maGiaoDich = deposit.MaGiaoDich,
            soTienCoc = TotalDeposit,
            bankName = $"{_sepay.BankAccount} ({_sepay.BankName})",
            bankNumber = _sepay.BankNumber,
            accountName = _sepay.AccountName,
            transferContent = maGiaoDich,
            tenPhienBan = Dong.TenDong,
            hoTen = HoTen,
            soDienThoai = SoDienThoai,
            dongCo = "",
            hopSo = "",
            mauSac = "",
            loaiNhietLieu = "",
            giaNiemYet = 0,
            soLuongXe = totalXe
        };

        TempData["DepositResult"] = JsonSerializer.Serialize(result);

        if (IsAjaxSubmit)
        {
            var bin = "970422";
            var qrUrl = CarProject.Services.VietQr.BuildDataUri(bin, _sepay.BankNumber, 10000, maGiaoDich);
            var tongTienGoc = selected.Sum(kv => (long)PhienBans.First(p => p.MaPhienBan == kv.Key).GiaNiemYet * kv.Value);
            var ajaxRateList = selected.Select(kv => PhienBans.First(p => p.MaPhienBan == kv.Key).IsPreOrder ? "15%" : "20%").Distinct().ToList();
            var ajaxRateText = ajaxRateList.Count == 1 ? ajaxRateList[0] : "15%/20%";
            return new JsonResult(new
            {
                success = true,
                maDonCoc = deposit.MaDonCoc,
                maGiaoDich = deposit.MaGiaoDich,
                soTienCoc = TotalDeposit,
                tongTienGoc,
                cocRateText = ajaxRateText,
                bankName = $"{_sepay.BankAccount} ({_sepay.BankName})",
                bankNumber = _sepay.BankNumber,
                accountName = _sepay.AccountName,
                paymentMethod = isCashPayment ? "Tiền mặt" : "Chuyển khoản",
                showQr = !isCashPayment,
                qrUrl = isCashPayment ? "" : qrUrl,
                message = isCashPayment
                    ? "Đơn đặt cọc đã được ghi nhận và đang chờ thanh toán. Admin/showroom sẽ xác nhận trong thời gian sớm nhất."
                    : "Đơn đặt cọc đã được ghi nhận. Vui lòng chuyển khoản để hoàn tất."
            });
        }

        return RedirectToPage("/Orders/DepositResult", new { maDonCoc = deposit.MaDonCoc });
    }

    public class PhienBanOption
    {
        public int MaPhienBan { get; set; }
        public string TenPhienBan { get; set; } = "";
        public long GiaNiemYet { get; set; }
        public int SoLuongTrongKho { get; set; }
        public int DaDatCoc { get; set; }
        public int ConLai { get; set; }
        public bool IsPreOrder { get; set; }
        public string MauSac { get; set; } = "";
        public string DongCo { get; set; } = "";
        public string HopSo { get; set; } = "";
    }

    private IActionResult JsonError(string message)
        => new JsonResult(new { success = false, error = message });
}
