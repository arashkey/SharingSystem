using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application.Common;
using SharingSystem.Web.Infrastructure;

namespace SharingSystem.Web.Controllers;

/// <summary>
/// Base class for MVC controllers with helpers for flash messages and validation errors.
/// </summary>
public abstract class AppController : Controller
{
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
            ModelState.AddModelError(string.Empty, exception.Message);
            return false;
        }
    }

    protected void FlashSuccess(string message) => TempData[WebConstants.SuccessMessageKey] = message;

    protected void FlashError(string message) => TempData[WebConstants.ErrorMessageKey] = message;

    /// <summary>Moves the first model state error into a flash message (used by forms that redirect).</summary>
    protected void FlashModelErrors()
    {
        var message = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
        if (!string.IsNullOrEmpty(message))
        {
            FlashError(message);
        }
    }

    protected IActionResult RedirectToLocal(string? returnUrl, string fallback = "/") =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : LocalRedirect(fallback);
}
