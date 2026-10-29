using System.Security.Claims;

namespace CarProject.Services;

public static class JwtCookieExtensions
{
    public const string CookieName = "MyLxCarJwt";

    public static void SetJwtCookie(this HttpContext ctx, string token, TimeSpan? expiry = null)
    {
        ctx.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(expiry ?? TimeSpan.FromMinutes(30)),
            Path = "/"
        });
    }

    public static void ClearJwtCookie(this HttpContext ctx)
    {
        ctx.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/" });
    }

    public static string? GetJwtFromCookie(this HttpContext ctx)
    {
        ctx.Request.Cookies.TryGetValue(CookieName, out var token);
        return token;
    }

    public static string? GetJwtUserName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.NameIdentifier);

    public static string? GetJwtDisplayName(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Name);

    public static string? GetJwtRole(this ClaimsPrincipal user)
        => user.FindFirstValue(ClaimTypes.Role);

    public static string? GetJwtAvatar(this ClaimsPrincipal user)
        => user.FindFirstValue("AvatarUrl");

    public static string? GetJwtEmail(this ClaimsPrincipal user)
        => user.FindFirstValue("Email");

    public static int? GetJwtMaTaiKhoan(this ClaimsPrincipal user)
    {
        var val = user.FindFirstValue("MaTaiKhoan");
        return int.TryParse(val, out var id) ? id : null;
    }

    public static bool IsJwtLoggedIn(this ClaimsPrincipal user)
        => user.Identity?.IsAuthenticated == true;
}
