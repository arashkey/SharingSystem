using System.Globalization;

namespace SharingSystem.Web.Localization;

/// <summary>
/// The user interface languages offered by the application.
/// </summary>
public static class SupportedCultures
{
    public const string English = "en";
    public const string Persian = "fa";
    public const string Default = English;

    /// <summary>
    /// Culture used for number and date parsing/formatting on the server. It stays fixed so that model binding
    /// does not depend on the selected UI language; dates are rendered in the browser in the user's language.
    /// </summary>
    public const string FormattingCulture = English;

    public static readonly IReadOnlyList<string> UiCultures = [English, Persian];

    /// <summary>Language names written in the language itself (used by the language switcher).</summary>
    private static readonly IReadOnlyDictionary<string, string> NativeNames = new Dictionary<string, string>
    {
        [English] = "English",
        [Persian] = "فارسی"
    };

    public static bool IsSupported(string? culture) =>
        culture is not null && UiCultures.Contains(culture, StringComparer.OrdinalIgnoreCase);

    public static string GetNativeName(string culture) => NativeNames[culture];

    /// <summary>Returns the supported UI culture that matches the current request (falls back to the default).</summary>
    public static string Current()
    {
        var name = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return IsSupported(name) ? name : Default;
    }

    /// <summary>The language a switcher should offer next to the current one.</summary>
    public static string Alternate() => Current() == Persian ? English : Persian;

    public static bool IsRightToLeft() => CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;
}
