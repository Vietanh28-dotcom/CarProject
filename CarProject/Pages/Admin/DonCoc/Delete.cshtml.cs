using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages.Admin.DonCoc;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IActivityLogService _log;

    [BindProperty]
    public DonDatCoc DonCoc { get; set; }

    public string ErrorMessage { get; set; }

    public DeleteModel(AppDbContext db, IActivityLogService log)
    {
        _db = db;
        _log = log;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        DonCoc = await _db.DonDatCoc.FindAsync(id);
        if (DonCoc == null)
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (DonCoc == null)
            return NotFound();

        var detail = $"Mã đơn={DonCoc.MaDonCoc}, tiền={DonCoc.SoTienCoc:N0}, trạng thái={DonCoc.TrangThaiDonHang}";
        try
        {
            _db.DonDatCoc.Remove(DonCoc);
            await _db.SaveChangesAsync();
            await _log.LogAsync("Admin Xóa đơn cọc", detail);
            return RedirectToPage("Index");
        }
        catch (DbUpdateException)
        {
            ErrorMessage = $"Không thể xóa đơn cọc #{DonCoc.MaDonCoc} vì có hóa đơn liên quan. Vui lòng xóa các hóa đơn liên quan trước.";
            return Page();
        }
    }
}
