using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Shares;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Controllers;

/// <summary>
/// "My shares" of the signed-in user.
/// </summary>
[Authorize]
[Route("account/shares")]
public sealed class SharesController(
    IShareManagementService managementService,
    IShareUploadService uploadService,
    IAppUrlResolver appUrlResolver) : AppController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = Pagination.FirstPage, CancellationToken cancellationToken = default) =>
        View(new ShareListViewModel
        {
            Shares = await managementService.GetMySharesAsync(page, Pagination.DefaultPageSize, cancellationToken),
            BaseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken)
        });

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(string id, int page = Pagination.FirstPage, CancellationToken cancellationToken = default)
    {
        var share = await managementService.GetForEditAsync(id, page, Pagination.DefaultPageSize, cancellationToken);
        return View(await BuildEditPageAsync(share, share.Adapt<EditShareForm>(), cancellationToken));
    }

    /// <summary>The page of the file list is kept, so the user stays where they were.</summary>
    [HttpPost("{id}/edit")]
    public async Task<IActionResult> Edit(
        string id,
        [Bind(Prefix = "Form")] EditShareForm form,
        int page = Pagination.FirstPage,
        CancellationToken cancellationToken = default)
    {
        if (ModelState.IsValid
            && await TryExecuteAsync(() => managementService.UpdateAsync(id, form.Adapt<UpdateShareRequest>(), cancellationToken)))
        {
            FlashSuccess("The share has been updated.");
            return RedirectToAction(nameof(Edit), new { id, page });
        }

        var share = await managementService.GetForEditAsync(id, page, Pagination.DefaultPageSize, cancellationToken);
        return View(await BuildEditPageAsync(share, form, cancellationToken));
    }

    [HttpPost("{id}/files/{fileId:guid}/delete")]
    public async Task<IActionResult> DeleteFile(string id, Guid fileId, string? returnUrl, CancellationToken cancellationToken)
    {
        if (await TryExecuteAsync(() => managementService.DeleteFileAsync(id, fileId, cancellationToken)))
        {
            FlashSuccess("The file has been deleted.");
        }
        else
        {
            FlashModelErrors();
        }

        return RedirectToLocal(returnUrl, Url.Action(nameof(Edit), new { id })!);
    }

    [HttpPost("{id}/delete")]
    public async Task<IActionResult> Delete(string id, string? returnUrl, CancellationToken cancellationToken)
    {
        await managementService.DeleteAsync(id, cancellationToken);
        FlashSuccess("The share has been deleted.");
        return RedirectToLocal(returnUrl, Url.Action(nameof(Index))!);
    }

    private async Task<EditSharePageViewModel> BuildEditPageAsync(
        ShareDetailsDto share, EditShareForm form, CancellationToken cancellationToken) =>
        new()
        {
            Share = share,
            Form = form,
            Limits = await uploadService.GetLimitsAsync(null, cancellationToken),
            BaseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken)
        };
}
