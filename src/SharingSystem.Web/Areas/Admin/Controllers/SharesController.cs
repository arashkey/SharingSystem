using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Shares;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Areas.Admin.Controllers;

[Route("admin/shares")]
public sealed class SharesController(
    IShareManagementService managementService,
    IAppUrlResolver appUrlResolver) : AdminController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new ShareListViewModel
        {
            Shares = await managementService.GetAllSharesAsync(cancellationToken),
            BaseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken)
        });

    [HttpPost("{id}/delete")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await managementService.DeleteAsync(id, cancellationToken);
        FlashSuccess("The share has been deleted.");
        return RedirectToAction(nameof(Index));
    }
}
