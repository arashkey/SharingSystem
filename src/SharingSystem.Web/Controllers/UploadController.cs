using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.ReverseShares;
using SharingSystem.Application.Settings;
using SharingSystem.Application.Shares;
using SharingSystem.Domain.Constants;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Controllers;

[AllowAnonymous]
[Route("upload")]
public sealed class UploadController(
    IShareUploadService uploadService,
    IReverseShareService reverseShareService,
    ISettingsService settingsService,
    IAppUrlResolver appUrlResolver) : AppController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (User.Identity?.IsAuthenticated != true && !settings.AllowUnauthenticatedShares)
        {
            return RedirectToAction("SignIn", "Auth", new { returnUrl = "/upload" });
        }

        return View(await BuildModelAsync(settings, null, null, false, cancellationToken));
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> ReverseShare(string token, CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        var reverseShare = await reverseShareService.GetForUploadAsync(token, cancellationToken);
        var model = await BuildModelAsync(
            settings, reverseShare.Token, reverseShare.CreatorUsername, reverseShare.IsSimplified, cancellationToken);
        return View(nameof(Index), model);
    }

    private async Task<UploadPageViewModel> BuildModelAsync(
        AppSettingsDto settings, string? reverseShareToken, string? creator, bool simplified, CancellationToken cancellationToken) =>
        new()
        {
            Limits = await uploadService.GetLimitsAsync(reverseShareToken, cancellationToken),
            BaseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken),
            SuggestedId = SecureTokens.CreateAlphaNumeric(DomainLimits.GeneratedShareIdLength),
            ReverseShareToken = reverseShareToken,
            ReverseShareCreator = creator,
            IsSimplified = simplified,
            CanSendToRecipients = settings.EnableShareEmailRecipients && settings.CanSendEmails
        };
}
