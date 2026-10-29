using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CarProject.Pages.Orders.Cart;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ICartService _cart;
    private readonly IActivityLogService _log;
    private readonly INotificationService _notif;

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
    public Dictionary<int, int> QuantityInput { get; set; } = new();

    [BindProperty]
    public Dictionary<int, string> SourceChiNhanh { get; set; } = new();

    [BindProperty]
    public string? DiaDiemGap { get; set; }
    [BindProperty]
    public string? ToaDoGap { get; set; }

    [BindProperty(Name = "_ajax")]
    public bool IsAjaxSubmit { get; set; }

    public List<CartItem> CartItems { get; set; } = new();
    public List<ChiNhanhShowroom> DanhSachChiNhanh { get; set; } = new();
    public Dictionary<int, List<TonKhoTheoChiNhanh>> TonKhoTheoPhienBan { get; set; } = new();
    public Dictionary<int, int> DaDatCocTheoPhienBanVaChiNhanh { get; set; } = new();
    public Dictionary<(int, string), int> TonKhoConLaiLut { get; set; } = new();
    public decimal TotalDeposit { get; set; }
    public string? ErrorMessage { get; set; }
    public string GoogleMapsApiKey { get; set; } = "";

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

    private Dictionary<(int, string), int> _reservedLut = new();
    private Dictionary<(int, string), int> _tonKhoLut = new();
    private Dictionary<string, string> _tenChiNhanhLut = new();

    public CheckoutModel(AppDbContext db, ICartService cart, IActivityLogService log, INotificationService notif, IConfiguration config)
    {
        _db = db;
        _cart = cart;
        _log = log;
        _notif = notif;
        GoogleMapsApiKey = config["GoogleMaps:ApiKey"]?.Trim() ?? "";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        CartItems = await _cart.GetCartAsync();
        if (CartItems.Count == 0)
            return RedirectToPage("/Orders/Cart/Index");

        if (!await _cart.HasMinimumQuantityAsync())
        {
            TempData["CartError"] = "Cần ít nhất 3 xe để đặt cọc theo giỏ hàng.";
            return RedirectToPage("/Orders/Cart/Index");
        }

        var currentUser = User.GetJwtUserName() ?? "";
        foreach (var item in CartItems)
        {
            var phienBan = await _db.PhienBanXe.FindAsync(item.MaPhienBan);
            if (phienBan == null) continue;

            var reservedByOthers = await _db.GioHang
                .Where(g => g.MaTaiKhoan != currentUser && g.MaPhienBan == item.MaPhienBan)
                .SumAsync(g => (int?)g.SoLuong) ?? 0;
            var depositReserved = await _db.DonDatCocChiTiet
                .Where(c => c.MaPhienBan == item.MaPhienBan
                    && c.DonDatCoc != null
                    && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                    && c.DonDatCoc!.TrangThaiDonHang != "Từ chối"
                    && c.DonDatCoc!.TrangThaiDonHang != "Đã từ chối"
                    && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất")
                .SumAsync(c => (int?)c.SoLuong) ?? 0;
            var available = Math.Max(0, phienBan.SoLuongTrongKho - reservedByOthers - depositReserved);
            if (item.SoLuong > available)
            {
                TempData["CartError"] = $"Số lượng không đủ cho xe \"{item.TenPhienBan}\". Hàng tồn của xe là: {available} xe.";
                return RedirectToPage("/Orders/Cart/Index");
            }
        }

        await LoadShowrooms();
        await LoadTonKhoAsync();
        await LoadReservedAsync(User.GetJwtUserName() ?? "");
        TotalDeposit = await _cart.GetTotalDepositAsync();
        QuantityInput = CartItems.ToDictionary(c => c.MaPhienBan, c => c.SoLuong);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CartItems = await _cart.GetCartAsync();
        if (CartItems.Count == 0)
            return RedirectToPage("/Orders/Cart/Index");

        if (!await _cart.HasMinimumQuantityAsync())
            return Fail("Cần ít nhất 3 xe để đặt cọc.");

        if (string.IsNullOrEmpty(HoTen) || string.IsNullOrEmpty(SoDienThoai) || string.IsNullOrEmpty(DiaChi))
        {
            if (IsAjaxSubmit) return JsonError("Vui lòng điền đầy đủ thông tin.");
            ErrorMessage = "Vui lòng điền đầy đủ thông tin.";
            await LoadShowrooms();
            return Page();
        }

        if (string.IsNullOrEmpty(MaChiNhanh))
        {
            if (IsAjaxSubmit) return JsonError("Vui lòng chọn showroom hẹn gặp / nhận xe.");
            ErrorMessage = "Vui lòng chọn showroom hẹn gặp / nhận xe.";
            await LoadShowrooms();
            await LoadTonKhoAsync();
            return Page();
        }

        var userName = User.GetJwtUserName();
        var groupCode = $"DH{DateTime.Now:yyMMddHHmmss}";

        // Áp dụng số lượng người dùng đã chọn
        foreach (var item in CartItems)
        {
            if (QuantityInput.TryGetValue(item.MaPhienBan, out var qty) && qty > 0 && qty <= item.SoLuong)
            {
                item.SoLuong = qty;
            }
        }

        TotalDeposit = CartItems.Sum(c => c.SoTienCoc * c.SoLuong);

        var pbIds = CartItems.Select(c => c.MaPhienBan).Distinct().ToList();

        // ===== Transaction + khoá dòng để chống bán vượt tồn kho khi đặt cọc đồng thời =====
        await using var tx = await _db.Database.BeginTransactionAsync();

        // Khoá tất cả dòng tồn kho của các phiên bản trong đơn (UPDLOCK + HOLDLOCK giữ tới hết transaction)
        foreach (var pbId in pbIds)
        {
            await _db.Database.ExecuteSqlRawAsync(
                "SELECT MaTonKho FROM TonKhoTheoChiNhanh WITH (UPDLOCK, HOLDLOCK) WHERE MaPhienBan = {0}", pbId);
        }

        // Sau khi đã khoá, đọc lại tồn kho + số lượng đã đặt cọc
        await LoadTonKhoLutAsync(pbIds);
        await LoadReservedLutAsync(pbIds, userName ?? "");
        await LoadTenChiNhanhLutAsync();

        // ===== Phân bổ showroom nguồn: ưu tiên showroom đã chọn / showroom hẹn gặp trước, sau đó mới sang showroom khác =====
        var allocated = new List<(CartItem item, List<(string cn, int qty)> parts)>();
        foreach (var item in CartItems)
        {
            var phienBan = await _db.PhienBanXe.FindAsync(item.MaPhienBan);
            if (phienBan == null)
            {
                ErrorMessage = $"Xe \"{item.TenPhienBan}\" không tồn tại.";
                await tx.RollbackAsync();
                await LoadShowrooms();
                return Page();
            }

            var userSource = SourceChiNhanh.TryGetValue(item.MaPhienBan, out var sc) && !string.IsNullOrEmpty(sc)
                ? sc
                : null;

            var reservedTotal = _reservedLut.Where(k => k.Key.Item1 == item.MaPhienBan).Sum(k => k.Value);
            var availableOverall = Math.Max(0, phienBan.SoLuongTrongKho - reservedTotal);
            if (item.SoLuong > availableOverall)
            {
                ErrorMessage = $"Phần xe \"{item.TenPhienBan}\" đã có người đặt cọc hoặc đang có người khác thêm vào giỏ hàng. Hiện còn {availableOverall} xe chưa bị đặt cọc.";
                await tx.RollbackAsync();
                await LoadShowrooms();
                await LoadTonKhoAsync();
                return Page();
            }

            var parts = AllocateSource(phienBan, item.SoLuong, userSource, MaChiNhanh);
            if (parts.Count == 0)
            {
                var available = phienBan.SoLuongTrongKho <= 0
                    ? 0
                    : _tonKhoLut.Where(k => k.Key.Item1 == item.MaPhienBan).Sum(k => k.Value)
                        - _reservedLut.Where(k => k.Key.Item1 == item.MaPhienBan).Sum(k => k.Value);
                ErrorMessage = $"Không đủ xe \"{item.TenPhienBan}\". Hiện chỉ còn {Math.Max(0, available)} xe trên toàn hệ thống.";
                await tx.RollbackAsync();
                await LoadShowrooms();
                await LoadTonKhoAsync();
                return Page();
            }

            allocated.Add((item, parts));
        }

        var totalXe = CartItems.Sum(c => c.SoLuong);
        var hasPreOrder = CartItems.Any(c => c.SoLuongTrongKho <= 0);
        var isCashPayment = string.Equals(PhuongThucThanhToan, "Tiền mặt", StringComparison.OrdinalIgnoreCase);

        if (isCashPayment)
        {
            if (string.IsNullOrWhiteSpace(DiaDiemGap))
                return Fail("Vui lòng chọn địa điểm nhân viên quản lý đến thu tiền cọc trên bản đồ.");
        }
        else
        {
            DiaDiemGap = null;
            ToaDoGap = null;
        }

        // ===== Tạo MỘT đơn cọc duy nhất =====
        // Tiền mặt -> chờ thanh toán; xe hết hàng -> chờ xử lý; xe còn hàng -> chờ xác nhận
        var deposit = new DonDatCoc
        {
            MaKhachHang = userName ?? "",
            MaChiNhanh = MaChiNhanh,
            SoTienCoc = TotalDeposit,
            PhuongThucThanhToan = PhuongThucThanhToan,
            TrangThaiThanhToan = isCashPayment ? "Chờ thanh toán" : "Chưa thanh toán",
            TrangThaiDonHang = isCashPayment ? "Chờ thanh toán" : (hasPreOrder ? "Chờ xử lý" : "Chờ xác nhận"),
            NgayTaoDon = DateTime.Now,
            HoTen = HoTen,
            SoDienThoai = SoDienThoai,
            DiaChi = DiaChi,
            GhiChu = GhiChu,
            DiaDiemGap = DiaDiemGap,
            ToaDoGap = ToaDoGap,
            MaGiaoDich = groupCode
        };

        // Thêm chi tiết từng xe (một xe có thể chia nhiều showroom nguồn) vào chung một đơn
foreach (var (item, parts) in allocated)
        {
            foreach (var (cn, qty) in parts)
            {
                var isPreOrder = item.SoLuongTrongKho <= 0;
                _tonKhoLut.TryGetValue((item.MaPhienBan, cn), out var tonKhoCn);
                _reservedLut.TryGetValue((item.MaPhienBan, cn), out var reservedCn);
                var soLuongThieu = isPreOrder ? qty : Math.Max(0, qty - Math.Max(0, tonKhoCn - reservedCn));

                deposit.ChiTiets.Add(new DonDatCocChiTiet
                {
                    MaPhienBan = item.MaPhienBan,
                    MaChiNhanh = cn,
                    SoLuong = qty,
                    SoTienCocPhanBo = DepositCalculator.Compute(item.GiaNiemYet, isPreOrder) * qty,
                    SoLuongThieu = soLuongThieu
                });
            }
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

        await tx.CommitAsync();
        await _cart.ClearCartAsync();

        await _log.LogAsync("Đặt cọc giỏ hàng",
            $"{HoTen} - {SoDienThoai} - {totalXe} xe - Tổng cọc: {TotalDeposit:N0}VNĐ - Nhận tại: {MaChiNhanh}");

        // Xoá giỏ hàng ngay sau khi tạo đơn đặt cọc thành công
        if (IsAjaxSubmit)
        {
            var sepay = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<CarProject.Services.SepaySettings>>();
            var s = sepay.Value;
            var bin = "970422";
            var qrUrl = CarProject.Services.VietQr.BuildDataUri(bin, s.BankNumber, 10000, groupCode);
            var tongTienGoc = CartItems.Sum(c => (long)c.GiaNiemYet * c.SoLuong);
            var rateList = CartItems.Select(c => c.SoLuongTrongKho <= 0 ? "15%" : "20%").Distinct().ToList();
            var ajaxRateText = rateList.Count == 1 ? rateList[0] : "15%/20%";
            return new JsonResult(new
            {
                success = true,
                maDonCoc = deposit.MaDonCoc,
                maGiaoDich = groupCode,
                soTienCoc = TotalDeposit,
                tongTienGoc,
                cocRateText = ajaxRateText,
                bankName = $"{s.BankAccount} ({s.BankName})",
                bankNumber = s.BankNumber,
                accountName = s.AccountName,
                paymentMethod = PhuongThucThanhToan,
                showQr = !isCashPayment,
                qrUrl = isCashPayment ? "" : qrUrl,
                orderStatus = isCashPayment ? "Chờ thanh toán" : "Chờ xác nhận",
                orderUrl = $"/Orders/DepositResult?maDonCoc={deposit.MaDonCoc}",
                message = isCashPayment
                    ? "Đơn đặt cọc đã được ghi nhận và đang chờ thanh toán. Admin/showroom sẽ xác nhận trong thời gian sớm nhất."
                    : "Đơn đặt cọc đã được ghi nhận. Vui lòng chuyển khoản để hoàn tất."
            });
        }

        return RedirectToPage("/Orders/DepositResult", new { maDonCoc = deposit.MaDonCoc });
    }

    // ===== Phân bổ nguồn: ưu tiên showroom đã chọn -> showroom hẹn gặp -> các showroom còn lại =====
    private List<(string cn, int qty)> AllocateSource(PhienBanXe pb, int qty, string? userSource, string receiveCn)
    {
        // Xe hết hàng: cho phép đặt trước, chọn bất kỳ showroom để chuẩn bị xe
        if (pb.SoLuongTrongKho <= 0)
        {
            var src = !string.IsNullOrEmpty(userSource) ? userSource : receiveCn;
            return new List<(string, int)> { (src, qty) };
        }

        // Chỉ cấp phát cho ĐÚNG MỘT showroom mỗi xe (showroom khách đã chọn / showroom nguồn).
        // KHÔNG tự phân chia sang showroom khác: nếu showroom này thiếu xe, SoLuongThieu sẽ được
        // ghi ở lúc tạo chi tiết → Quản Lý showroom đó chủ động "Nhập hàng" hoặc "Từ chối", thay vì
        // đơn tự động lấn sang showroom khác khiến đơn bị tách thành nhiều showroom.
        var target = !string.IsNullOrEmpty(userSource) ? userSource : receiveCn;
        if (string.IsNullOrEmpty(target)) return new List<(string, int)>();
        return new List<(string, int)> { (target, qty) };
    }

    private async Task LoadShowrooms()
    {
        DanhSachChiNhanh = await _db.ChiNhanhShowroom
            .Where(c => c.TrangThai == "Active" || c.TrangThai == "Hoạt động")
            .ToListAsync();
        foreach (var cn in DanhSachChiNhanh)
            _tenChiNhanhLut[cn.MaChiNhanh] = cn.TenChiNhanh;
    }

    private async Task LoadTonKhoAsync()
    {
        var maPhienBans = CartItems.Select(c => c.MaPhienBan).ToList();
        var tonKhoList = await _db.TonKhoTheoChiNhanh
            .Include(t => t.ChiNhanh)
            .Where(t => maPhienBans.Contains(t.MaPhienBan))
            .ToListAsync();
        TonKhoTheoPhienBan = tonKhoList.GroupBy(t => t.MaPhienBan)
            .ToDictionary(g => g.Key, g => g.ToList());
        _tonKhoLut = new Dictionary<(int, string), int>();
        foreach (var r in tonKhoList)
            _tonKhoLut[(r.MaPhienBan, r.MaChiNhanh)] = r.SoLuong;
    }

    private async Task LoadReservedAsync(string currentUser)
    {
        var maPhienBans = CartItems.Select(c => c.MaPhienBan).ToList();
        await LoadReservedLutAsync(maPhienBans, currentUser);
        foreach (var kv in _reservedLut)
        {
            if (!DaDatCocTheoPhienBanVaChiNhanh.ContainsKey(kv.Key.Item1))
                DaDatCocTheoPhienBanVaChiNhanh[kv.Key.Item1] = 0;
            DaDatCocTheoPhienBanVaChiNhanh[kv.Key.Item1] += kv.Value;
            TonKhoConLaiLut[kv.Key] = (_tonKhoLut.TryGetValue(kv.Key, out var tk) ? tk : 0) - kv.Value;
        }
        foreach (var tk in _tonKhoLut)
        {
            if (!_reservedLut.ContainsKey(tk.Key))
                TonKhoConLaiLut[tk.Key] = tk.Value;
        }
    }

    private async Task LoadReservedLutAsync(List<int> pbIds, string currentUser)
    {
        var depositRows = await _db.DonDatCocChiTiet
            .Where(c => pbIds.Contains(c.MaPhienBan)
                && c.DonDatCoc != null
                && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                && c.DonDatCoc!.TrangThaiDonHang != "Từ chối"
                && c.DonDatCoc!.TrangThaiDonHang != "Đã từ chối"
                && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất")
            .Select(c => new { c.MaPhienBan, c.MaChiNhanh, c.SoLuong })
            .ToListAsync();

        var cartRows = await _db.GioHang
            .Where(g => pbIds.Contains(g.MaPhienBan) && !string.IsNullOrEmpty(currentUser) && g.MaTaiKhoan != currentUser)
            .Select(g => new { g.MaPhienBan, MaChiNhanh = (string?)null, g.SoLuong })
            .ToListAsync();

        _reservedLut = new Dictionary<(int, string), int>();
        foreach (var r in depositRows)
        {
            var key = (r.MaPhienBan, r.MaChiNhanh ?? "");
            _reservedLut.TryGetValue(key, out var cur);
            _reservedLut[key] = cur + r.SoLuong;
        }

        foreach (var r in cartRows)
        {
            var key = (r.MaPhienBan, "");
            _reservedLut.TryGetValue(key, out var cur);
            _reservedLut[key] = cur + r.SoLuong;
        }
    }

    private async Task LoadTonKhoLutAsync(List<int> pbIds)
    {
        var rows = await _db.TonKhoTheoChiNhanh
            .Where(t => pbIds.Contains(t.MaPhienBan))
            .ToListAsync();
        _tonKhoLut = new Dictionary<(int, string), int>();
        foreach (var r in rows)
        {
            _tonKhoLut[(r.MaPhienBan, r.MaChiNhanh)] = r.SoLuong;
        }
    }

    private async Task LoadTenChiNhanhLutAsync()
    {
        var rows = await _db.ChiNhanhShowroom.ToListAsync();
        _tenChiNhanhLut = rows.ToDictionary(c => c.MaChiNhanh, c => c.TenChiNhanh);
    }

    private string TenChiNhanh(string? maCn)
        => _tenChiNhanhLut.TryGetValue(maCn ?? "", out var t) ? t : (maCn ?? "");

    private IActionResult JsonError(string message)
        => new JsonResult(new { success = false, error = message });

    private IActionResult Fail(string message)
    {
        if (IsAjaxSubmit) return JsonError(message);
        ErrorMessage = message;
        return Page();
    }
}
