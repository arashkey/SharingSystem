using Mapster;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Notifications;
using SharingSystem.Application.Settings;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Areas.Admin.Controllers;

[Route("admin/settings")]
public sealed class SettingsController(
    ISettingsService settingsService,
    INotificationService notificationService) : AdminController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return View(settings.Adapt<SettingsForm>());
    }

    [HttpPost("")]
    public async Task<IActionResult> Index(SettingsForm form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid
            && await TryExecuteAsync(() => settingsService.UpdateAsync(form.Adapt<UpdateAppSettingsRequest>(), cancellationToken)))
        {
            FlashSuccess("The settings have been saved.");
            return RedirectToAction(nameof(Index));
        }

        var current = await settingsService.GetAsync(cancellationToken);
        form.HasSmtpPassword = current.HasSmtpPassword;
        form.NewSmtpPassword = null;
        return View(form);
    }

    [HttpPost("test-email")]
    public async Task<IActionResult> TestEmail(TestEmailForm form, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid
            && await TryExecuteAsync(() => notificationService.SendTestEmailAsync(form.Email, cancellationToken)))
        {
            FlashSuccess($"A test e-mail has been sent to {form.Email}.");
        }
        else
        {
            FlashModelErrors();
        }

        return RedirectToAction(nameof(Index));
    }
}
