using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Settings;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Controllers;

public sealed class HomeController(ISettingsService settingsService) : AppController
{
    [HttpGet("/")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (settings.ShowHomePage)
        {
            return View(settings);
        }

        return User.Identity?.IsAuthenticated == true || settings.AllowUnauthenticatedShares
            ? RedirectToAction("Index", "Upload")
            : RedirectToAction("SignIn", "Auth");
    }

    [HttpGet("/imprint")]
    public Task<IActionResult> Imprint(CancellationToken cancellationToken) =>
        LegalAsync("Imprint", s => s.ImprintText, cancellationToken);

    [HttpGet("/privacy")]
    public Task<IActionResult> Privacy(CancellationToken cancellationToken) =>
        LegalAsync("Privacy policy", s => s.PrivacyPolicyText, cancellationToken);

    [HttpGet("/error/{statusCode:int?}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode)
    {
        var code = statusCode ?? StatusCodes.Status500InternalServerError;
        var hasException = HttpContext.Features.Get<IExceptionHandlerPathFeature>() is not null;
        var message = code switch
        {
            StatusCodes.Status404NotFound => "The page you are looking for does not exist.",
            StatusCodes.Status403Forbidden => "You are not allowed to access this page.",
            StatusCodes.Status413PayloadTooLarge => "The uploaded content is too large.",
            _ when hasException => "An unexpected error occurred.",
            _ => "Something went wrong."
        };

        Response.StatusCode = code;
        return View(new ErrorViewModel { StatusCode = code, Message = message });
    }

    private async Task<IActionResult> LegalAsync(string title, Func<AppSettingsDto, string?> selector, CancellationToken cancellationToken)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        var text = selector(settings);
        if (!settings.LegalEnabled || string.IsNullOrWhiteSpace(text))
        {
            return NotFound();
        }

        ViewData["Title"] = title;
        return View("Legal", text);
    }
}
