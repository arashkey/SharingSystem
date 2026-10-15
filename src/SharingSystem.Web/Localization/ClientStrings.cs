using Microsoft.Extensions.Localization;

namespace SharingSystem.Web.Localization;

/// <summary>
/// Texts used by the browser scripts. The layout renders their translations as JSON and <c>app.t()</c> reads them.
/// </summary>
public static class ClientStrings
{
    private static readonly string[] Keys =
    [
        "Copied to clipboard",
        "Loading…",
        "The preview could not be loaded.",
        "Remove",
        "{0} file(s) · {1}",
        " — exceeds the limit of {0}",
        "Request failed ({0})",
        "Upload failed ({0})",
        "Network error",
        "Waiting…",
        "Done",
        "Failed",
        "This link is already in use."
    ];

    public static IReadOnlyDictionary<string, string> Translate(IStringLocalizer localizer) =>
        Keys.ToDictionary(key => key, key => localizer[key].Value);
}
