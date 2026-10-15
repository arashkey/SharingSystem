using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using SharingSystem.Application.Common;
using SharingSystem.Web.Infrastructure;
using SharingSystem.Web.Localization;

namespace SharingSystem.Web.Controllers;

/// <summary>
/// Base class for MVC controllers with helpers for flash messages and validation errors.
/// </summary>
public abstract class AppController : Controller
{
    private IStringLocalizer? localizer;

    /// <summary>Shared UI texts; resolved per request so derived controllers keep their own constructors.</summary>
    protected IStringLocalizer Localizer =>
        localizer ??= HttpContext.RequestServices.GetRequiredService<IStringLocalizer<SharedResource>>();

    /// <summary>
    /// Runs an action and turns validation errors into model state errors.
    /// Returns <c>true</c> when the action succeeded.
    /// </summary>
    protected async Task<bool> TryExecuteAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (AppException exception) when (exception.ErrorType == AppErrorType.Validation)
        {
            ModelState.AddModelError(string.Empty, Localizer.Localize(exception));
            return false;
        }
    }

    /// <summary>Shows a translated success message after the next redirect.</summary>
    protected void FlashSuccess(string messageTemplate, params object[] arguments) =>
        TempData[WebConstants.SuccessMessageKey] = Translate(messageTemplate, arguments);

    /// <summary>Shows a translated error message after the next redirect.</summary>
    protected void FlashError(string messageTemplate, params object[] arguments) =>
        TempData[WebConstants.ErrorMessageKey] = Translate(messageTemplate, arguments);

    /// <summary>Moves the first model state error into a flash message (used by forms that redirect).</summary>
    protected void FlashModelErrors()
    {
        // Model state errors are already translated, so they are stored as they are.
        var message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
        if (!string.IsNullOrEmpty(message))
        {
            TempData[WebConstants.ErrorMessageKey] = message;
        }
    }

    protected IActionResult RedirectToLocal(string? returnUrl, string fallback = "/") =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect(fallback);

    private string Translate(string messageTemplate, object[] arguments) =>
        arguments.Length == 0 ? Localizer[messageTemplate].Value : Localizer[messageTemplate, arguments].Value;
}
