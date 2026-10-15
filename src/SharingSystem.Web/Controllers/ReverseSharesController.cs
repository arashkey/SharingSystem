using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.ReverseShares;
using SharingSystem.Application.Settings;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Controllers;

[Authorize]
[Route("account/reverse-shares")]
public sealed class ReverseSharesController(
    IReverseShareService reverseShareService,
    ISettingsService settingsService,
    IAppUrlResolver appUrlResolver) : AppController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = Pagination.FirstPage, CancellationToken cancellationToken = default) =>
        View(await BuildPageAsync(new CreateReverseShareForm(), page, cancellationToken));

    [HttpPost("")]
    public async Task<IActionResult> Create([Bind(Prefix = "Form")] CreateReverseShareForm form, CancellationToken cancellationToken)
    {
        ReverseShareDto? created = null;
        if (ModelState.IsValid
            && await TryExecuteAsync(async () =>
                created = await reverseShareService.CreateAsync(form.Adapt<CreateReverseShareRequest>(), cancellationToken)))
        {
            FlashSuccess("Reverse share created: {0}", created!.Url);
            return RedirectToAction(nameof(Index));
        }

        // The new reverse share would be the newest one, so the form is shown on the first page.
        return View(nameof(Index), await BuildPageAsync(form, Pagination.FirstPage, cancellationToken));
    }

    [HttpPost("{id:guid}/delete")]
    public async Task<IActionResult> Delete(Guid id, string? returnUrl, CancellationToken cancellationToken)
    {
        await reverseShareService.DeleteAsync(id, cancellationToken);
        FlashSuccess("The reverse share has been deleted. Shares created with it are kept.");
        return RedirectToLocal(returnUrl, Url.Action(nameof(Index))!);
    }

    private async Task<ReverseSharesPageViewModel> BuildPageAsync(
        CreateReverseShareForm form, int page, CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return new ReverseSharesPageViewModel
        {
            ReverseShares = await reverseShareService.GetMineAsync(page, Pagination.DefaultPageSize, cancellationToken),
            Form = form,
            BaseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken),
            MaxShareSizeBytes = settings.MaxShareSizeBytes,
            CanSendEmails = settings.CanSendEmails
        };
    }
}
