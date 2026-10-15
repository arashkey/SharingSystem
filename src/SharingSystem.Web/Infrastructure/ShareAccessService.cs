using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace SharingSystem.Web.Infrastructure;

/// <summary>
/// Remembers that a visitor was granted access to a share (password entered / visit counted),
/// using a protected cookie that is only sent to the share's own URLs.
/// </summary>
public sealed class ShareAccessService(
    IHttpContextAccessor httpContextAccessor,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<CookieLifetimeOptions> cookieOptions)
{
    private const string Purpose = "SharingSystem.ShareAccess";

    private HttpContext HttpContext =>
        httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");

    private ITimeLimitedDataProtector Protector =>
        dataProtectionProvider.CreateProtector(Purpose).ToTimeLimitedDataProtector();

    public bool HasAccess(string shareId)
    {
        if (!HttpContext.Request.Cookies.TryGetValue(GetCookieName(shareId), out var value))
        {
            return false;
        }

        try
        {
            return Protector.Unprotect(value) == shareId;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public void Grant(string shareId)
    {
        var lifetime = TimeSpan.FromHours(cookieOptions.Value.ShareAccessHours);
        HttpContext.Response.Cookies.Append(GetCookieName(shareId), Protector.Protect(shareId, lifetime), new CookieOptions
        {
            HttpOnly = true,
            Secure = HttpContext.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = $"{HttpContext.Request.PathBase}/s/{shareId}",
            MaxAge = lifetime
        });
    }

    private static string GetCookieName(string shareId) => WebConstants.ShareAccessCookiePrefix + shareId;
}
