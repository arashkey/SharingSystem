using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SharingSystem.Application.Auth;

namespace SharingSystem.Web.Infrastructure;

/// <summary>
/// Validates every authentication cookie against the database so that deleted users,
/// changed passwords or revoked admin rights take effect immediately.
/// </summary>
public static class SessionValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var stamp = principal?.FindFirstValue(WebConstants.SecurityStampClaim);
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) || stamp is null)
        {
            await RejectAsync(context);
            return;
        }

        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        var user = await authService.GetSessionUserAsync(userId, stamp, context.HttpContext.RequestAborted);
        if (user is null)
        {
            await RejectAsync(context);
            return;
        }

        var changed = principal!.Identity?.Name != user.Username
                      || principal.FindFirstValue(ClaimTypes.Email) != user.Email
                      || principal.IsInRole(WebConstants.AdminRole) != user.IsAdmin;
        if (changed)
        {
            context.ReplacePrincipal(SessionService.CreatePrincipal(user));
            context.ShouldRenew = true;
        }
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
