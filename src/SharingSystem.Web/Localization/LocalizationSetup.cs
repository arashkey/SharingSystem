using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.WebEncoders;

namespace SharingSystem.Web.Localization;

/// <summary>
/// Registers UI localization: shared string resources, translated validation messages and the request culture
/// (taken from the language cookie, otherwise from the browser's Accept-Language header).
/// </summary>
public static class LocalizationSetup
{
    public static IServiceCollection AddAppLocalization(this IServiceCollection services)
    {
        services.AddLocalization();
        // Without this, every non-Latin character (e.g. Persian text) is written as an HTML entity.
        services.Configure<WebEncoderOptions>(options =>
            options.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
        // Post-configuration runs after MVC's own setup, so the validation attributes are already collected
        // when LocalizedValidationMetadataProvider runs.
        services.AddSingleton<IPostConfigureOptions<MvcOptions>, ConfigureModelBindingMessages>();
        services.PostConfigure<MvcOptions>(options =>
            options.ModelMetadataDetailsProviders.Add(new LocalizedValidationMetadataProvider()));

        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.SetDefaultCulture(SupportedCultures.Default);
            options.AddSupportedCultures(SupportedCultures.FormattingCulture);
            options.AddSupportedUICultures([.. SupportedCultures.UiCultures]);
            options.RequestCultureProviders =
            [
                new CookieRequestCultureProvider(),
                new AcceptLanguageHeaderRequestCultureProvider()
            ];
        });

        return services;
    }

    /// <summary>Configures data annotations so display names and validation messages use the shared resources.</summary>
    public static IMvcBuilder AddAppDataAnnotationsLocalization(this IMvcBuilder builder) =>
        builder.AddDataAnnotationsLocalization(options =>
            options.DataAnnotationLocalizerProvider = (_, factory) => factory.Create(typeof(SharedResource)));
}
