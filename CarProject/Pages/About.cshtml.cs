using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Services;

namespace CarProject.Pages;

public class AboutModel : PageModel
{
    private readonly IActivityLogService _log;

    public AboutModel(IActivityLogService log)
    {
        _log = log;
    }

    public async Task OnGetAsync()
    {
        await _log.LogAsync("Xem trang giới thiệu");
    }
}
