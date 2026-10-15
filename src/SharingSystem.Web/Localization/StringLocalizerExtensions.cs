using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using SharingSystem.Application.Common;

namespace SharingSystem.Web.Localization;

public static partial class StringLocalizerExtensions
{
    /// <summary>Translates the message of a business error, filling in its placeholders.</summary>
    public static string Localize(this IStringLocalizer localizer, AppException exception) =>
        exception.MessageArguments.Length == 0
            ? localizer[exception.MessageTemplate].Value
            : localizer[exception.MessageTemplate, exception.MessageArguments].Value;

    /// <summary>Translated display name of an enum value; the English text is derived from the member name.</summary>
    public static string EnumDisplayName<TEnum>(this IStringLocalizer localizer, TEnum value)
        where TEnum : struct, Enum =>
        localizer[Humanize(value.ToString())].Value;

    /// <summary>Select list items for an enum with translated texts and numeric values.</summary>
    public static IEnumerable<SelectListItem> EnumSelectList<TEnum>(this IStringLocalizer localizer)
        where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(value => new SelectListItem(
            localizer.EnumDisplayName(value),
            Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)));

    /// <summary>"NoCompression" becomes "No compression".</summary>
    private static string Humanize(string memberName)
    {
        var words = WordBoundary().Split(memberName);
        return string.Join(' ', words.Select((word, index) => index == 0 ? word : word.ToLowerInvariant()));
    }

    [GeneratedRegex("(?<=[a-z])(?=[A-Z])")]
    private static partial Regex WordBoundary();
}
