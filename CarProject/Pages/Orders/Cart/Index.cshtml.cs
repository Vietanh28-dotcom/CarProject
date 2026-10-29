using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Services;
using CarProject.Data;
using Microsoft.EntityFrameworkCore;

namespace CarProject.Pages.Orders.Cart;

public class IndexModel : PageModel
{
    private readonly ICartService _cart;
    private readonly AppDbContext _db;

    public List<CartItem> CartItems { get; set; } = new();
    public decimal TotalDeposit { get; set; }
    public int ItemCount { get; set; }

    public IndexModel(ICartService cart, AppDbContext db)
    {
        _cart = cart;
        _db = db;
    }

    public async Task OnGetAsync()
    {
        CartItems = await _cart.GetCartAsync();
        ItemCount = CartItems.Sum(c => c.SoLuong);
        TotalDeposit = await _cart.GetTotalDepositAsync();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int maPhienBan)
    {
        await _cart.RemoveFromCartAsync(maPhienBan);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateQuantityAsync(int maPhienBan, int soLuong)
    {
        if (soLuong > 0)
        {
            var phienBan = await _db.PhienBanXe.FindAsync(maPhienBan);
            if (phienBan != null)
            {
                var currentUser = User.GetJwtUserName() ?? "";
                var reservedByOthers = await _db.GioHang
                    .Where(g => g.MaTaiKhoan != currentUser && g.MaPhienBan == maPhienBan)
                    .SumAsync(g => (int?)g.SoLuong) ?? 0;
                var depositReserved = await _db.DonDatCocChiTiet
                    .Where(c => c.MaPhienBan == maPhienBan
                        && c.DonDatCoc != null
                        && c.DonDatCoc!.TrangThaiThanhToan == "Đã thanh toán"
                        && c.DonDatCoc!.TrangThaiDonHang != "Đã hủy"
                        && c.DonDatCoc!.TrangThaiDonHang != "Đã huỷ"
                        && c.DonDatCoc!.TrangThaiDonHang != "Từ chối"
                        && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất")
                    .SumAsync(c => (int?)c.SoLuong) ?? 0;
                var available = Math.Max(0, phienBan.SoLuongTrongKho - reservedByOthers - depositReserved);
                if (soLuong > available)
                {
                    TempData["CartError"] = $"Số lượng không đủ cho xe này. Hàng tồn của xe là: {available} xe.";
                    return RedirectToPage();
                }
            }
        }
        await _cart.UpdateQuantityAsync(maPhienBan, soLuong);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostClearAsync()
    {
        await _cart.ClearCartAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCheckoutAsync()
    {
        var items = await _cart.GetCartAsync();
        var totalItems = items.Sum(c => c.SoLuong);
        if (totalItems < 3)
        {
            TempData["CartError"] = "Cần ít nhất 3 xe để tiến hành đặt cọc theo giỏ hàng.";
            return RedirectToPage();
        }

        var currentUser = User.GetJwtUserName() ?? "";
        foreach (var item in items)
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
                    && c.DonDatCoc!.TrangThaiDonHang != "Hoàn tất")
                .SumAsync(c => (int?)c.SoLuong) ?? 0;
            var available = Math.Max(0, phienBan.SoLuongTrongKho - reservedByOthers - depositReserved);
            if (item.SoLuong > available)
            {
                TempData["CartError"] = $"Số lượng không đủ cho xe \"{item.TenPhienBan}\". Hàng tồn của xe là: {available} xe.";
                return RedirectToPage();
            }
        }

        return RedirectToPage("/Orders/Cart/Checkout");
    }
}
