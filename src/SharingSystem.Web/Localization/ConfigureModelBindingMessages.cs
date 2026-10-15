using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace SharingSystem.Web.Localization;

/// <summary>
/// Translates the model binding error messages (e.g. a non-numeric value in a number field).
/// </summary>
public sealed class ConfigureModelBindingMessages(IStringLocalizer<SharedResource> localizer) : IPostConfigureOptions<MvcOptions>
{
    public void PostConfigure(string? name, MvcOptions options)
    {
        var messages = options.ModelBindingMessageProvider;
        messages.SetValueIsInvalidAccessor(value => localizer["The value '{0}' is invalid.", value]);
        messages.SetValueMustNotBeNullAccessor(value => localizer["The value '{0}' is invalid.", value]);
        messages.SetAttemptedValueIsInvalidAccessor((value, field) => localizer["The value '{0}' is not valid for {1}.", value, field]);
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => localizer["The value '{0}' is not valid.", value]);
        messages.SetUnknownValueIsInvalidAccessor(field => localizer["The supplied value is invalid for {0}.", field]);
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => localizer["The supplied value is invalid."]);
        messages.SetValueMustBeANumberAccessor(field => localizer["The field {0} must be a number.", field]);
        messages.SetNonPropertyValueMustBeANumberAccessor(() => localizer["The field must be a number."]);
        messages.SetMissingBindRequiredValueAccessor(field => localizer["A value for the '{0}' parameter or property was not provided.", field]);
        messages.SetMissingKeyOrValueAccessor(() => localizer["A value is required."]);
        messages.SetMissingRequestBodyRequiredValueAccessor(() => localizer["A non-empty request body is required."]);
    }
}
