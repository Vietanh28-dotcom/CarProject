using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using CarProject.Data;
using CarProject.Models;
using CarProject.Services;

namespace CarProject.Pages;

public class Viewer360Model : PageModel
{
    private readonly IActivityLogService _log;

    public Viewer360Model(IActivityLogService log)
    {
        _log = log;
    }

    public List<DongXe> DanhSachXe { get; set; } = new();
    public DongXe? SelectedXe { get; set; }
    public int SelectedId { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        await _log.LogAsync("Truy cập trang xem 360 đã bị loại bỏ");
        return RedirectToPage("/Cars");
    }
}
