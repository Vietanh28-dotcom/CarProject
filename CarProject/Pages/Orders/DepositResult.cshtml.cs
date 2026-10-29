using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CarProject.Pages.Orders;

public class DepositResultModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly SepaySettings _sepay;

    public int MaDonCoc { get; set; }
    public string MaGiaoDich { get; set; } = "";
    public decimal SoTienCoc { get; set; }
    public string SoTienCocStr { get; set; } = "";
    public string BankName { get; set; } = "";
    public string BankNumber { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string TransferContent { get; set; } = "";
    public string QrImageUrl { get; set; } = "";
    public string TenPhienBan { get; set; } = "";
    public string HoTen { get; set; } = "";
    public bool ShowQr { get; set; }
    public bool IsCashPayment { get; set; }
    public string DongCo { get; set; } = "";
    public string HopSo { get; set; } = "";
    public string MauSac { get; set; } = "";
    public string LoaiNhietLieu { get; set; } = "";
    public long GiaNiemYet { get; set; }
    public string GiaNiemYetStr { get; set; } = "";
    public string TongTienGocStr { get; set; } = "";
    public string CocRateText { get; set; } = "20%";
    public string TrangThaiDonHang { get; set; } = "";
    public string SoDienThoai { get; set; } = "";
    public List<DonDatCocChiTiet> ChiTiets { get; set; } = new();
    public string TrangThaiThanhToan { get; set; } = "";
    public string DiaDiemGap { get; set; } = "";
    public string ToaDoGap { get; set; } = "";
    public string ShowroomTen { get; set; } = "";
    public string ShowroomDiaChi { get; set; } = "";
    public string ShowroomThanhPho { get; set; } = "";

    public bool IsFailed =>
        string.Equals(TrangThaiDonHang, "Đã hủy", StringComparison.OrdinalIgnoreCase)
        || string.Equals(TrangThaiDonHang, "Đã huỷ", StringComparison.OrdinalIgnoreCase)
        || string.Equals(TrangThaiDonHang, "Đã từ chối", StringComparison.OrdinalIgnoreCase);

    // Đã chuyển tiền / nhận tiền cọc nhưng đơn vẫn bị từ chối/huỷ → phải hoàn tiền
    public bool IsPaidBeforeFail =>
        IsFailed && string.Equals(TrangThaiThanhToan, "Đã thanh toán", StringComparison.OrdinalIgnoreCase);

    // Đã nhận tiền đặt cọc (chuyển khoản webhook báo có, hoặc QL xác nhận nhận tiền mặt)
    public bool IsMoneyReceived =>
        string.Equals(TrangThaiThanhToan, "Đã thanh toán", StringComparison.OrdinalIgnoreCase);

    // Tiền mặt chưa được QL xác nhận nhận tiền cọc
    public bool IsCashPending => IsCashPayment && !IsMoneyReceived;

    // Chuyển khoản chưa thanh toán xong
    public bool IsTransferPending => !IsCashPayment && !IsMoneyReceived
        && string.Equals(TrangThaiDonHang, "Chờ thanh toán", StringComparison.OrdinalIgnoreCase);

    public DepositResultModel(AppDbContext db, IOptions<SepaySettings> sepay)
    {
        _db = db;
        _sepay = sepay.Value;
    }

    public async Task<IActionResult> OnGetAsync(int? maDonCoc)
    {
        var raw = TempData["DepositResult"] as string;
        if (!string.IsNullOrEmpty(raw))
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            MaDonCoc = root.GetProperty("maDonCoc").GetInt32();
            MaGiaoDich = root.GetProperty("maGiaoDich").GetString() ?? "";
            SoTienCoc = root.GetProperty("soTienCoc").GetDecimal();
            BankName = root.GetProperty("bankName").GetString() ?? "";
            BankNumber = root.GetProperty("bankNumber").GetString() ?? "";
            AccountName = root.GetProperty("accountName").GetString() ?? "";
            TransferContent = root.GetProperty("transferContent").GetString() ?? "";
            TenPhienBan = root.GetProperty("tenPhienBan").GetString() ?? "";
            HoTen = root.GetProperty("hoTen").GetString() ?? "";
            IsCashPayment = root.TryGetProperty("paymentMethod", out var paymentMethod) && string.Equals(paymentMethod.GetString(), "Tiền mặt", StringComparison.OrdinalIgnoreCase);
            SoDienThoai = root.TryGetProperty("soDienThoai", out var sdt) ? sdt.GetString() ?? "" : "";
            DongCo = root.TryGetProperty("dongCo", out var dc) ? dc.GetString() ?? "" : "";
            HopSo = root.TryGetProperty("hopSo", out var hs) ? hs.GetString() ?? "" : "";
            MauSac = root.TryGetProperty("mauSac", out var ms) ? ms.GetString() ?? "" : "";
            LoaiNhietLieu = root.TryGetProperty("loaiNhietLieu", out var lnl) ? lnl.GetString() ?? "" : "";
            GiaNiemYet = root.TryGetProperty("giaNiemYet", out var gny) ? gny.GetInt64() : 0;
        }
        else if (maDonCoc.HasValue)
        {
            // Load từ DB nếu vào từ notification link
            var don = await _db.DonDatCoc
                .Include(d => d.PhienBan).ThenInclude(p => p.DongXe)
                .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
                .Include(d => d.ChiTiets).ThenInclude(c => c.ChiNhanh)
                .FirstOrDefaultAsync(d => d.MaDonCoc == maDonCoc.Value);
            if (don == null) return RedirectToPage("/Index");

            MaDonCoc = don.MaDonCoc;
            MaGiaoDich = don.MaGiaoDich ?? "";
            SoTienCoc = don.SoTienCoc;
            BankName = $"{_sepay.BankAccount} ({_sepay.BankName})";
            BankNumber = _sepay.BankNumber;
            AccountName = _sepay.AccountName;
            TransferContent = don.MaGiaoDich ?? "";
            ChiTiets = don.ChiTiets.ToList();
            TenPhienBan = string.Join(", ", don.ChiTiets.Select(c =>
                $"{c.PhienBan?.DongXe?.TenDong ?? ""} {c.PhienBan?.TenPhienBan ?? ""}".Trim()).Where(s => s.Length > 0));
            if (string.IsNullOrEmpty(TenPhienBan))
                TenPhienBan = $"{don.PhienBan?.DongXe?.TenDong ?? ""} {don.PhienBan?.TenPhienBan ?? ""}".Trim();
            HoTen = don.HoTen ?? "";
            SoDienThoai = don.SoDienThoai ?? "";
            TrangThaiDonHang = don.TrangThaiDonHang ?? "";
            IsCashPayment = string.Equals(don.PhuongThucThanhToan, "Tiền mặt", StringComparison.OrdinalIgnoreCase);
            var firstPb = don.ChiTiets.Select(c => c.PhienBan).FirstOrDefault(p => p != null) ?? don.PhienBan;
            if (firstPb != null)
            {
                DongCo = firstPb.DongCo ?? "";
                HopSo = firstPb.HopSo ?? "";
                MauSac = firstPb.MauSac ?? "";
                LoaiNhietLieu = firstPb.LoaiNhietLieu ?? "";
                GiaNiemYet = firstPb.GiaNiemYet;
            }
        }
        else
        {
            return RedirectToPage("/Index");
        }

        // Bổ sung thông tin đầy đủ từ DB (mọi đường dẫn): trạng thái, chi tiết, showroom gặp mặt
        if (MaDonCoc > 0)
        {
            var don4 = await _db.DonDatCoc
                .Include(d => d.ChiTiets).ThenInclude(c => c.PhienBan).ThenInclude(p => p.DongXe)
                .Include(d => d.ChiTiets).ThenInclude(c => c.ChiNhanh)
                .Include(d => d.ChiNhanh)
                .FirstOrDefaultAsync(d => d.MaDonCoc == MaDonCoc);
            if (don4 != null)
            {
                TrangThaiDonHang = don4.TrangThaiDonHang ?? TrangThaiDonHang;
                TrangThaiThanhToan = don4.TrangThaiThanhToan ?? TrangThaiThanhToan;
                if (!ChiTiets.Any()) ChiTiets = don4.ChiTiets.ToList();
                DiaDiemGap = don4.DiaDiemGap ?? "";
                ToaDoGap = don4.ToaDoGap ?? "";
                ShowroomTen = don4.ChiNhanh?.TenChiNhanh ?? "";
                ShowroomDiaChi = don4.ChiNhanh?.DiaChi ?? "";
                ShowroomThanhPho = don4.ChiNhanh?.ThanhPho ?? "";
            }
        }

        SoTienCocStr = SoTienCoc >= 1_000_000_000
            ? $"{SoTienCoc / 1_000_000_000m:N2} tỷ VNĐ"
            : $"{SoTienCoc / 1_000_000:N0} triệu VNĐ";
        GiaNiemYetStr = GiaNiemYet >= 1_000_000_000
            ? $"{GiaNiemYet / 1_000_000_000m:N2} tỷ VNĐ"
            : $"{GiaNiemYet / 1_000_000:N0} triệu VNĐ";

        var tongGoc = ChiTiets.Sum(c => (long)(c.PhienBan?.GiaNiemYet ?? 0) * c.SoLuong);
        if (tongGoc <= 0) tongGoc = GiaNiemYet;
        TongTienGocStr = tongGoc >= 1_000_000_000
            ? $"{tongGoc / 1_000_000_000m:N2} tỷ VNĐ"
            : $"{tongGoc / 1_000_000:N0} triệu VNĐ";
        var rateItems = ChiTiets
            .Select(c => c.PhienBan != null && c.PhienBan.SoLuongTrongKho <= 0 ? "15%" : "20%")
            .Distinct().ToList();
        if (rateItems.Count == 0) rateItems.Add("20%");
        CocRateText = rateItems.Count == 1 ? rateItems[0] : "15%/20%";

        // Hiện QR để khách quét thanh toán khi chưa chuyển tiền xong
        if (!IsCashPayment && !IsMoneyReceived && !string.IsNullOrEmpty(MaGiaoDich))
        {
            var bin = "970422"; // MB Bank BIN
            QrImageUrl = CarProject.Services.VietQr.BuildDataUri(bin, _sepay.BankNumber, 10000, MaGiaoDich);
            ShowQr = true;
        }

        return Page();
    }
}
