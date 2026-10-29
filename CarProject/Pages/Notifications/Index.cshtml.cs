using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarProject.Services;

namespace CarProject.Pages.Notifications;

public class IndexModel : PageModel
{
    private readonly INotificationService _notif;

    public List<Models.ThongBao> Notifications { get; set; } = new();
    public int UnreadCount { get; set; }

    public IndexModel(INotificationService notif)
    {
        _notif = notif;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userName = User.GetJwtUserName();
        if (string.IsNullOrEmpty(userName))
            return RedirectToPage("/Account/Login");

        Notifications = await _notif.GetRecentAsync(userName, 50);
        UnreadCount = await _notif.GetUnreadCountAsync(userName);
        return Page();
    }

    public async Task<IActionResult> OnPostMarkReadAsync(int id)
    {
        await _notif.MarkAsReadAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllReadAsync()
    {
        var userName = User.GetJwtUserName();
        if (!string.IsNullOrEmpty(userName))
            await _notif.MarkAllAsReadAsync(userName);
        return RedirectToPage();
    }
}
