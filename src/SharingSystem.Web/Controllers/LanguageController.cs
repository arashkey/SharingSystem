using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Web.Localization;

namespace SharingSystem.Web.Controllers;

/// <summary>
/// Stores the UI language chosen by the visitor in a cookie.
/// </summary>
[AllowAnonymous]
[Route("language")]
public sealed class LanguageController : AppController
{
    private static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(365);

    [HttpPost("")]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (SupportedCultures.IsSupported(culture))
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture.ToLowerInvariant())),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.Add(CookieLifetime),
                    IsEssential = true,
                    HttpOnly = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps
                });
        }

        return RedirectToLocal(returnUrl);
    }
}
