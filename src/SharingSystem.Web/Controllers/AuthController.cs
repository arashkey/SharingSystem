using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SharingSystem.Application.Auth;
using SharingSystem.Application.Settings;
using SharingSystem.Web.Infrastructure;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Controllers;

[AllowAnonymous]
[Route("auth")]
public sealed class AuthController(
    IAuthService authService,
    ISettingsService settingsService,
    SessionService sessionService) : AppController
{
    [HttpGet("sign-in")]
    public async Task<IActionResult> SignIn(string? returnUrl, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        if (await authService.IsInitialSetupAsync(cancellationToken))
        {
            return RedirectToAction(nameof(SignUp));
        }

        return View(await BuildSignInModelAsync(new SignInViewModel { ReturnUrl = returnUrl }, cancellationToken));
    }

    [HttpPost("sign-in")]
    [EnableRateLimiting(WebConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> SignIn(SignInViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(await BuildSignInModelAsync(model, cancellationToken));
        }

        CredentialsResult? result = null;
        if (!await TryExecuteAsync(async () =>
                result = await authService.ValidateCredentialsAsync(model.EmailOrUsername, model.Password, cancellationToken)))
        {
            return View(await BuildSignInModelAsync(model, cancellationToken));
        }

        if (result!.RequiresTotp)
        {
            sessionService.StartTwoFactor(result.User.Id);
            return RedirectToAction(nameof(Totp), new { returnUrl = model.ReturnUrl });
        }

        await sessionService.SignInAsync(result.User);
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet("totp")]
    public IActionResult Totp(string? returnUrl)
    {
        if (sessionService.GetPendingTwoFactorUserId() is null)
        {
            return RedirectToAction(nameof(SignIn), new { returnUrl });
        }

        return View(new TotpViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("totp")]
    [EnableRateLimiting(WebConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> Totp(TotpViewModel model, CancellationToken cancellationToken)
    {
        var userId = sessionService.GetPendingTwoFactorUserId();
        if (userId is null)
        {
            FlashError("The sign-in session expired. Please sign in again.");
            return RedirectToAction(nameof(SignIn), new { returnUrl = model.ReturnUrl });
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AuthenticatedUser? user = null;
        if (!await TryExecuteAsync(async () => user = await authService.VerifyTotpAsync(userId.Value, model.Code, cancellationToken)))
        {
            return View(model);
        }

        await sessionService.SignInAsync(user!);
        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpGet("sign-up")]
    public async Task<IActionResult> SignUp(CancellationToken cancellationToken)
    {
        if (!await authService.CanRegisterAsync(cancellationToken))
        {
            FlashError("Registration is disabled.");
            return RedirectToAction(nameof(SignIn));
        }

        return View(new SignUpViewModel { IsInitialSetup = await authService.IsInitialSetupAsync(cancellationToken) });
    }

    [HttpPost("sign-up")]
    [EnableRateLimiting(WebConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> SignUp(SignUpViewModel model, CancellationToken cancellationToken)
    {
        model.IsInitialSetup = await authService.IsInitialSetupAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        AuthenticatedUser? user = null;
        if (!await TryExecuteAsync(async () => user = await authService.RegisterAsync(model.Adapt<RegisterRequest>(), cancellationToken)))
        {
            return View(model);
        }

        await sessionService.SignInAsync(user!);
        FlashSuccess(user!.IsAdmin ? "Welcome! You are the administrator of this instance." : "Welcome!");
        return user.IsAdmin ? Redirect("/admin/settings") : RedirectToAction("Index", "Upload");
    }

    [HttpPost("sign-out")]
    public async Task<IActionResult> SignOutUser()
    {
        await sessionService.SignOutAsync();
        return Redirect("/");
    }

    [HttpGet("forgot-password")]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost("forgot-password")]
    [EnableRateLimiting(WebConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid
            || !await TryExecuteAsync(() => authService.RequestPasswordResetAsync(model.Email, cancellationToken)))
        {
            return View(model);
        }

        FlashSuccess("If an account with this e-mail exists, a reset link has been sent.");
        return RedirectToAction(nameof(SignIn));
    }

    [HttpGet("reset-password/{token}")]
    public IActionResult ResetPassword(string token) => View(new ResetPasswordViewModel { Token = token });

    [HttpPost("reset-password/{token}")]
    [EnableRateLimiting(WebConstants.AuthRateLimitPolicy)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid
            || !await TryExecuteAsync(() => authService.ResetPasswordAsync(model.Token, model.Password, cancellationToken)))
        {
            return View(model);
        }

        FlashSuccess("Your password has been changed. You can sign in now.");
        return RedirectToAction(nameof(SignIn));
    }

    private async Task<SignInViewModel> BuildSignInModelAsync(SignInViewModel model, CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        model.CanRegister = await authService.CanRegisterAsync(cancellationToken);
        model.CanResetPassword = settings.CanSendEmails;
        model.Password = string.Empty;
        return model;
    }
}
