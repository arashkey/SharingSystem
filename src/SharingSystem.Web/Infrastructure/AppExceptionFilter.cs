using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using SharingSystem.Application.Common;
using SharingSystem.Web.Localization;
using SharingSystem.Web.Models;

namespace SharingSystem.Web.Infrastructure;

/// <summary>
/// Converts <see cref="AppException"/> into JSON errors for API controllers and into the error page for MVC controllers.
/// The message is translated into the language of the request.
/// </summary>
public sealed class AppExceptionFilter(
    IModelMetadataProvider modelMetadataProvider,
    IStringLocalizer<SharedResource> localizer) : IExceptionFilter
{
    private const string ErrorViewName = "Error";

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AppException exception)
        {
            return;
        }

        var statusCode = exception.ErrorType switch
        {
            AppErrorType.NotFound => StatusCodes.Status404NotFound,
            AppErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status400BadRequest
        };

        var message = localizer.Localize(exception);
        var isApi = context.ActionDescriptor.EndpointMetadata.OfType<ApiControllerAttribute>().Any();
        if (isApi)
        {
            context.Result = new ObjectResult(new ApiError(message)) { StatusCode = statusCode };
        }
        else
        {
            var viewData = new ViewDataDictionary<ErrorViewModel>(modelMetadataProvider, context.ModelState)
            {
                Model = new ErrorViewModel { StatusCode = statusCode, Message = message }
            };
            context.Result = new ViewResult { ViewName = ErrorViewName, ViewData = viewData, StatusCode = statusCode };
        }

        context.ExceptionHandled = true;
    }
}

public sealed record ApiError(string Message);
