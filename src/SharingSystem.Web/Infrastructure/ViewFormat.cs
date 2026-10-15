using System.Globalization;
using SharingSystem.Application.Common;

namespace SharingSystem.Web.Infrastructure;

/// <summary>
/// Formatting helpers for Razor views.
/// </summary>
public static class ViewFormat
{
    private const string IsoFormat = "o";

    public static string Bytes(long bytes) => ByteSize.Format(bytes);

    /// <summary>ISO 8601 value that the browser converts to local time (see site.js).</summary>
    public static string Iso(DateTime utc) => utc.ToString(IsoFormat, CultureInfo.InvariantCulture);

    public static bool IsExpired(DateTime? expiresAt) => expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow;
}
