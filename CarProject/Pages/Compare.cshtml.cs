using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages;

public class CarWinStats
{
    public bool KieuDang { get; set; }
    public bool Gia { get; set; }
    public bool DongCo { get; set; }
    public bool HopSo { get; set; }
    public bool NhienLieu { get; set; }
    public bool MauSac { get; set; }
    public bool TonKho { get; set; }
    public bool TrangThai { get; set; }
}

public class CompareModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    public CompareModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public List<DongXe> AllCars { get; set; } = new();
    public DongXe? CarLeft { get; set; }
    public DongXe? CarRight { get; set; }
    public string? ImageLeft { get; set; }
    public string? ImageRight { get; set; }
    public decimal? PriceLeft { get; set; }
    public decimal? PriceRight { get; set; }
    public string? EngineLeft { get; set; }
    public string? EngineRight { get; set; }
    public string? TransLeft { get; set; }
    public string? TransRight { get; set; }
    public string? FuelLeft { get; set; }
    public string? FuelRight { get; set; }
    public string? ColorLeft { get; set; }
    public string? ColorRight { get; set; }
    public int? StockLeft { get; set; }
    public int? StockRight { get; set; }
    public string? StatusLeft { get; set; }
    public string? StatusRight { get; set; }
    public CarWinStats LeftWins { get; set; } = new();
    public CarWinStats RightWins { get; set; } = new();
    public int LeftScore { get; set; }
    public int RightScore { get; set; }

    public async Task OnGetAsync(int? left, int? right)
    {
        AllCars = await _db.DongXe.Include(d => d.HangXe).ToListAsync();

        if (left.HasValue && left > 0)
        {
            CarLeft = await _db.DongXe.Include(d => d.HangXe).Include(d => d.PhienBanXes)
                .FirstOrDefaultAsync(d => d.MaDong == left);
            if (CarLeft != null)
            {
                var pb = CarLeft.PhienBanXes?.FirstOrDefault();
                ImageLeft = CarLeft.DuongDanAnh ?? pb?.DuongDanAnh;
                PriceLeft = pb?.GiaNiemYet;
                EngineLeft = pb?.DongCo;
                TransLeft = pb?.HopSo;
                FuelLeft = pb?.LoaiNhietLieu;
                ColorLeft = pb?.MauSac;
                StockLeft = pb?.SoLuongTrongKho;
                StatusLeft = pb?.TrangThai;
            }
        }

        if (right.HasValue && right > 0)
        {
            CarRight = await _db.DongXe.Include(d => d.HangXe).Include(d => d.PhienBanXes)
                .FirstOrDefaultAsync(d => d.MaDong == right);
            if (CarRight != null)
            {
                var pb = CarRight.PhienBanXes?.FirstOrDefault();
                ImageRight = CarRight.DuongDanAnh ?? pb?.DuongDanAnh;
                PriceRight = pb?.GiaNiemYet;
                EngineRight = pb?.DongCo;
                TransRight = pb?.HopSo;
                FuelRight = pb?.LoaiNhietLieu;
                ColorRight = pb?.MauSac;
                StockRight = pb?.SoLuongTrongKho;
                StatusRight = pb?.TrangThai;
            }
        }

        if (CarLeft != null && CarRight != null)
        {
            CalculateWins();
        }

        await _log.LogAsync("Xem trang so sánh xe");
    }

    private void CalculateWins()
    {
        // Kiểu dáng: so sánh chuỗi alphabetically (không có hơn/kém, cùng loại = hòa)
        var kd = string.Compare(CarLeft!.KieuDang ?? "", CarRight!.KieuDang ?? "", StringComparison.OrdinalIgnoreCase);
        if (kd > 0) { LeftWins.KieuDang = true; }
        else if (kd < 0) { RightWins.KieuDang = true; }
        // else hòa — cả false

        // Giá: cao hơn = thắng (premium hơn)
        if (PriceLeft.HasValue && PriceRight.HasValue)
        {
            if (PriceLeft > PriceRight) { LeftWins.Gia = true; }
            else if (PriceRight > PriceLeft) { RightWins.Gia = true; }
        }
        else if (PriceLeft.HasValue) { LeftWins.Gia = true; }
        else if (PriceRight.HasValue) { RightWins.Gia = true; }

        // Động cơ: dung tích lớn hơn = thắng (nếu parse được số)
        LeftWins.DongCo = CompareStringNumeric(EngineLeft, EngineRight, out var rDongCo);
        RightWins.DongCo = rDongCo;

        // Hộp số: so chuỗi
        var hs = string.Compare(TransLeft ?? "", TransRight ?? "", StringComparison.OrdinalIgnoreCase);
        if (hs > 0) { LeftWins.HopSo = true; }
        else if (hs < 0) { RightWins.HopSo = true; }

        // Nhiên liệu: so chuỗi
        var nl = string.Compare(FuelLeft ?? "", FuelRight ?? "", StringComparison.OrdinalIgnoreCase);
        if (nl > 0) { LeftWins.NhienLieu = true; }
        else if (nl < 0) { RightWins.NhienLieu = true; }

        // Màu sắc: so chuỗi
        var ms = string.Compare(ColorLeft ?? "", ColorRight ?? "", StringComparison.OrdinalIgnoreCase);
        if (ms > 0) { LeftWins.MauSac = true; }
        else if (ms < 0) { RightWins.MauSac = true; }

        // Tồn kho: nhiều hơn = thắng
        if (StockLeft.HasValue && StockRight.HasValue)
        {
            if (StockLeft > StockRight) { LeftWins.TonKho = true; }
            else if (StockRight > StockLeft) { RightWins.TonKho = true; }
        }
        else if (StockLeft.HasValue) { LeftWins.TonKho = true; }
        else if (StockRight.HasValue) { RightWins.TonKho = true; }

        // Trạng thái: so chuỗi
        var tt = string.Compare(StatusLeft ?? "", StatusRight ?? "", StringComparison.OrdinalIgnoreCase);
        if (tt > 0) { LeftWins.TrangThai = true; }
        else if (tt < 0) { RightWins.TrangThai = true; }

        // Tính điểm
        LeftScore = CountTrue(LeftWins);
        RightScore = CountTrue(RightWins);
    }

    private static int CountTrue(CarWinStats s)
    {
        int c = 0;
        if (s.KieuDang) c++;
        if (s.Gia) c++;
        if (s.DongCo) c++;
        if (s.HopSo) c++;
        if (s.NhienLieu) c++;
        if (s.MauSac) c++;
        if (s.TonKho) c++;
        if (s.TrangThai) c++;
        return c;
    }

    private static bool CompareStringNumeric(string? a, string? b, out bool bWins)
    {
        bWins = false;
        var numA = ExtractNumber(a);
        var numB = ExtractNumber(b);
        if (numA.HasValue && numB.HasValue)
        {
            if (numA > numB) return true;
            if (numB > numA) { bWins = true; }
        }
        else if (numA.HasValue) return true;
        else if (numB.HasValue) bWins = true;
        return false;
    }

    private static decimal? ExtractNumber(string? s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        var digits = new string(s.Where(c => char.IsDigit(c) || c == '.').ToArray());
        if (decimal.TryParse(digits, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val))
            return val;
        return null;
    }
}