using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using SharingSystem.Application.Auth;
using SharingSystem.Application.Settings;

namespace SharingSystem.Web.Infrastructure;

/// <summary>
/// Creates authentication cookies and the short-lived cookie used between password and TOTP verification.
/// </summary>
public sealed class SessionService(
    IHttpContextAccessor httpContextAccessor,
    ISettingsService settingsService,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<CookieLifetimeOptions> cookieOptions,
    TimeProvider timeProvider)
{
    private const string TwoFactorPurpose = "SharingSystem.TwoFactor";

    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");

    private ITimeLimitedDataProtector TwoFactorProtector =>
        dataProtectionProvider.CreateProtector(TwoFactorPurpose).ToTimeLimitedDataProtector();

    public static ClaimsPrincipal CreatePrincipal(AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(WebConstants.SecurityStampClaim, user.SecurityStamp)
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, WebConstants.AdminRole));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public async Task SignInAsync(AuthenticatedUser user)
    {
        var settings = await settingsService.GetAsync(HttpContext.RequestAborted);
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = timeProvider.GetUtcNow().AddHours(settings.SessionDurationHours),
            AllowRefresh = false
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, CreatePrincipal(user), properties);
        HttpContext.Response.Cookies.Delete(WebConstants.TwoFactorCookieName);
    }

    public Task SignOutAsync() => HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    public void StartTwoFactor(Guid userId)
    {
        var lifetime = TimeSpan.FromMinutes(cookieOptions.Value.TwoFactorMinutes);
        var value = TwoFactorProtector.Protect(userId.ToString(), lifetime);
        HttpContext.Response.Cookies.Append(WebConstants.TwoFactorCookieName, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            MaxAge = lifetime
        });
    }

    public Guid? GetPendingTwoFactorUserId()
    {
        if (!HttpContext.Request.Cookies.TryGetValue(WebConstants.TwoFactorCookieName, out var value))
        {
            return null;
        }

        try
        {
            return Guid.TryParse(TwoFactorProtector.Unprotect(value), out var userId) ? userId : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return null;
        }
    }
}
