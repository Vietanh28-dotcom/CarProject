using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Services;

namespace CarProject.Pages;

public class ContactModel : PageModel
{
    private readonly IActivityLogService _log;

    public ContactModel(IActivityLogService log)
    {
        _log = log;
    }

    [BindProperty]
    public string HoTen { get; set; } = "";

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string SoDienThoai { get; set; } = "";

    [BindProperty]
    public string ChuDe { get; set; } = "Tư vấn mua xe";

    [BindProperty]
    public string NoiDung { get; set; } = "";

    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        await _log.LogAsync("Xem trang liên hệ");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await _log.LogAsync($"Gửi yêu cầu liên hệ: {HoTen} - {Email} - {ChuDe}");
        SuccessMessage = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi trong thời gian sớm nhất.";
        return Page();
    }
}
