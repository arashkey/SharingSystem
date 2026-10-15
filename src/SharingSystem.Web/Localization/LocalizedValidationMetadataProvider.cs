using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace SharingSystem.Web.Localization;

/// <summary>
/// Gives the standard validation attributes an explicit English error message when none is set.
/// Data annotations localization only translates explicit messages, so this makes the default
/// messages translatable as well (the English text is the resource key).
/// </summary>
public sealed class LocalizedValidationMetadataProvider : IValidationMetadataProvider
{
    private const string RequiredMessage = "The {0} field is required.";
    private const string StringLengthMessage = "The field {0} must be a string with a maximum length of {1}.";
    private const string StringLengthWithMinimumMessage =
        "The field {0} must be a string with a minimum length of {2} and a maximum length of {1}.";
    private const string RangeMessage = "The field {0} must be between {1} and {2}.";
    private const string EmailAddressMessage = "The {0} field is not a valid e-mail address.";
    private const string UrlMessage = "The {0} field is not a valid fully-qualified http, https, or ftp URL.";

    public void CreateValidationMetadata(ValidationMetadataProviderContext context)
    {
        foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (!string.IsNullOrEmpty(attribute.ErrorMessage)
                || !string.IsNullOrEmpty(attribute.ErrorMessageResourceName)
                || attribute.ErrorMessageResourceType is not null)
            {
                continue;
            }

            var message = attribute switch
            {
                RequiredAttribute => RequiredMessage,
                StringLengthAttribute { MinimumLength: > 0 } => StringLengthWithMinimumMessage,
                StringLengthAttribute => StringLengthMessage,
                RangeAttribute => RangeMessage,
                EmailAddressAttribute => EmailAddressMessage,
                UrlAttribute => UrlMessage,
                _ => null
            };

            if (message is not null)
            {
                attribute.ErrorMessage = message;
            }
        }
    }
}
