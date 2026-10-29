using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CarProject.Pages.Admin;

public class ToggleModeModel : PageModel
{
    public IActionResult OnGet(string mode, string? returnUrl)
    {
        if (mode == "View" || mode == "Control")
        {
            Response.Cookies.Append("AdminMode", mode, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                Path = "/"
            });
        }

        var redirectUrl = returnUrl ?? "/";
        return Redirect(redirectUrl);
    }
}
