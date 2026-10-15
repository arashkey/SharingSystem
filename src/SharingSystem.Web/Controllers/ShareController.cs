using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Shares;
using SharingSystem.Web.Infrastructure;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Controllers;

/// <summary>
/// The public share page and file downloads.
/// </summary>
[AllowAnonymous]
[Route("s/{id}")]
public sealed class ShareController(
    IShareViewerService viewerService,
    ShareAccessService shareAccess,
    IAppUrlResolver appUrlResolver) : AppController
{
    private const string PlainTextContentType = "text/plain; charset=utf-8";
    private const string PdfContentType = "application/pdf";
    private const string SvgContentType = "image/svg+xml";
    private static readonly string[] InlineMediaPrefixes = ["image/", "video/", "audio/"];
    private static readonly string[] TextContentTypes = ["application/json", "application/xml", "application/javascript"];

    [HttpGet("")]
    public async Task<IActionResult> Show(string id, CancellationToken cancellationToken)
    {
        var view = await viewerService.GetAsync(id, cancellationToken);
        if (view.CanManage || shareAccess.HasAccess(id))
        {
            return await ShareViewAsync(view, cancellationToken);
        }

        if (view.Share.HasPassword)
        {
            return View("Password", new SharePasswordViewModel { ShareId = id });
        }

        await viewerService.RegisterVisitAsync(id, null, cancellationToken);
        shareAccess.Grant(id);
        return await ShareViewAsync(view, cancellationToken);
    }

    [HttpPost("unlock")]
    [EnableRateLimiting(WebConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> Unlock(string id, SharePasswordViewModel model, CancellationToken cancellationToken)
    {
        model.ShareId = id;
        if (!ModelState.IsValid
            || !await TryExecuteAsync(() => viewerService.RegisterVisitAsync(id, model.Password, cancellationToken)))
        {
            model.Password = string.Empty;
            return View("Password", model);
        }

        shareAccess.Grant(id);
        return RedirectToAction(nameof(Show), new { id });
    }

    [HttpGet("files/{fileId:guid}")]
    public async Task<IActionResult> Download(string id, Guid fileId, bool inline, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(id, cancellationToken);
        var file = await viewerService.OpenFileAsync(id, fileId, cancellationToken);

        if (!inline)
        {
            return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
        }

        var contentType = GetPreviewContentType(file.ContentType);
        if (contentType is null)
        {
            return File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: true);
        }

        if (contentType != PdfContentType)
        {
            // Previews must never run scripts in the context of this site.
            Response.Headers.ContentSecurityPolicy = "sandbox";
        }

        Response.Headers.ContentDisposition = new ContentDisposition { Inline = true, FileName = file.FileName }.ToString();
        return File(file.Content, contentType, enableRangeProcessing: true);
    }

    [HttpGet("zip")]
    public async Task<IActionResult> DownloadAll(string id, CancellationToken cancellationToken)
    {
        await EnsureAccessAsync(id, cancellationToken);
        var archive = await viewerService.OpenArchiveAsync(id, cancellationToken);
        return File(archive.Content, archive.ContentType, archive.FileName, enableRangeProcessing: true);
    }

    private async Task<IActionResult> ShareViewAsync(ShareViewDto view, CancellationToken cancellationToken)
    {
        var baseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken);
        return View("Show", new ShareViewModel { View = view, ShareUrl = $"{baseUrl}/s/{view.Share.Id}" });
    }

    private async Task EnsureAccessAsync(string id, CancellationToken cancellationToken)
    {
        if (shareAccess.HasAccess(id))
        {
            return;
        }

        var view = await viewerService.GetAsync(id, cancellationToken);
        if (!view.CanManage)
        {
            throw AppException.Forbidden("Open the share page first to get access to its files.");
        }
    }

    /// <summary>Returns the content type to use for an inline preview, or <c>null</c> when the type must be downloaded.</summary>
    private static string? GetPreviewContentType(string contentType)
    {
        if (contentType == SvgContentType)
        {
            return null;
        }

        if (contentType == PdfContentType || InlineMediaPrefixes.Any(p => contentType.StartsWith(p, StringComparison.Ordinal)))
        {
            return contentType;
        }

        if (contentType.StartsWith("text/", StringComparison.Ordinal) || TextContentTypes.Contains(contentType))
        {
            return PlainTextContentType;
        }

        return null;
    }
}
