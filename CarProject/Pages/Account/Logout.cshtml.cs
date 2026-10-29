using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Services;

namespace CarProject.Pages.Account;

public class LogoutModel : PageModel
{
    private readonly IActivityLogService _log;

    public LogoutModel(IActivityLogService log)
    {
        _log = log;
    }

    public async Task<IActionResult> OnGet()
    {
        var name = User.GetJwtUserName() ?? "(unknown)";
        await _log.LogAsync("Đăng xuất", $"Tài khoản \"{name}\"");
        HttpContext.ClearJwtCookie();
        foreach (var cookie in Request.Cookies.Keys)
        {
            if (cookie != JwtCookieExtensions.CookieName)
                Response.Cookies.Delete(cookie);
        }
        return RedirectToPage("/Index");
    }
}
