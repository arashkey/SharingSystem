using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Account;
using SharingSystem.Application.Auth;
using SharingSystem.Web.Infrastructure;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Controllers;

[Authorize]
[Route("account")]
public sealed class AccountController(IAccountService accountService, SessionService sessionService) : AppController
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await BuildPageAsync(null, cancellationToken));

    [HttpPost("profile")]
    public Task<IActionResult> UpdateProfile(ProfileForm form, CancellationToken cancellationToken) =>
        ExecuteAndRefreshSessionAsync(
            () => accountService.UpdateProfileAsync(form.Adapt<UpdateProfileRequest>(), cancellationToken),
            "Your profile has been updated.");

    [HttpPost("password")]
    public Task<IActionResult> ChangePassword(ChangePasswordForm form, CancellationToken cancellationToken) =>
        ExecuteAndRefreshSessionAsync(
            () => accountService.ChangePasswordAsync(form.Adapt<ChangePasswordRequest>(), cancellationToken),
            "Your password has been changed.");

    [HttpPost("totp/setup")]
    public async Task<IActionResult> StartTotpSetup(TotpForm form, CancellationToken cancellationToken)
    {
        TotpSetupDto? setup = null;
        if (!await TryExecuteAsync(async () =>
                setup = await accountService.StartTotpSetupAsync(form.Password ?? string.Empty, cancellationToken)))
        {
            FlashModelErrors();
            return RedirectToAction(nameof(Index));
        }

        return View(nameof(Index), await BuildPageAsync(setup, cancellationToken));
    }

    [HttpPost("totp/enable")]
    public Task<IActionResult> EnableTotp(TotpForm form, CancellationToken cancellationToken) =>
        ExecuteAndRefreshSessionAsync(
            () => accountService.EnableTotpAsync(form.Code ?? string.Empty, cancellationToken),
            "Two-factor authentication is now enabled.");

    [HttpPost("totp/disable")]
    public Task<IActionResult> DisableTotp(TotpForm form, CancellationToken cancellationToken) =>
        ExecuteAndRefreshSessionAsync(
            () => accountService.DisableTotpAsync(form.Password ?? string.Empty, form.Code ?? string.Empty, cancellationToken),
            "Two-factor authentication is now disabled.");

    private async Task<IActionResult> ExecuteAndRefreshSessionAsync(Func<Task<AuthenticatedUser>> action, string successMessage)
    {
        if (!ModelState.IsValid)
        {
            FlashModelErrors();
            return RedirectToAction(nameof(Index));
        }

        AuthenticatedUser? user = null;
        if (await TryExecuteAsync(async () => user = await action()))
        {
            // Security data changed, so the session cookie must be issued again.
            await sessionService.SignInAsync(user!);
            FlashSuccess(successMessage);
        }
        else
        {
            FlashModelErrors();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<AccountPageViewModel> BuildPageAsync(TotpSetupDto? totpSetup, CancellationToken cancellationToken)
    {
        var account = await accountService.GetAsync(cancellationToken);
        return new AccountPageViewModel
        {
            Account = account,
            Profile = account.Adapt<ProfileForm>(),
            TotpSetup = totpSetup
        };
    }
}
