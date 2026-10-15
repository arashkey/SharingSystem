using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Shares;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Areas.Admin.Controllers;

[Route("admin/shares")]
public sealed class SharesController(
    IShareManagementService managementService,
    IAppUrlResolver appUrlResolver) : AdminController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = Pagination.FirstPage, CancellationToken cancellationToken = default) =>
        View(new ShareListViewModel
        {
            Shares = await managementService.GetAllSharesAsync(page, Pagination.DefaultPageSize, cancellationToken),
            BaseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken)
        });

    [HttpPost("{id}/delete")]
    public async Task<IActionResult> Delete(string id, string? returnUrl, CancellationToken cancellationToken)
    {
        await managementService.DeleteAsync(id, cancellationToken);
        FlashSuccess("The share has been deleted.");
        return RedirectToLocal(returnUrl, Url.Action(nameof(Index))!);
    }
}
