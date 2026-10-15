using System.ComponentModel.DataAnnotations;

namespace SharingSystem.Application.Options;

/// <summary>
/// Upload limits that depend on the hosting environment (bound from the "Upload" section).
/// </summary>
public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    private const long DefaultMaxRequestBodyBytes = 100L * 1024 * 1024;
    private const long MinimumRequestBodyBytes = 1024L * 1024;

    /// <summary>Maximum size of a single HTTP request body. Upload chunks must not exceed this value.</summary>
    [Range(MinimumRequestBodyBytes, long.MaxValue)]
    public long MaxRequestBodyBytes { get; set; } = DefaultMaxRequestBodyBytes;
}

/// <summary>
/// Settings of the background job that removes expired data (bound from the "Cleanup" section).
/// </summary>
public sealed class CleanupOptions
{
    public const string SectionName = "Cleanup";

    private const int DefaultIntervalMinutes = 60;
    private const int DefaultIncompleteShareMaxAgeHours = 24;
    private const int MaxIntervalMinutes = 7 * 24 * 60;
    private const int MaxIncompleteShareMaxAgeHours = 30 * 24;

    [Range(1, MaxIntervalMinutes)]
    public int IntervalMinutes { get; set; } = DefaultIntervalMinutes;

    /// <summary>Shares that were never completed are removed after this many hours.</summary>
    [Range(1, MaxIncompleteShareMaxAgeHours)]
    public int IncompleteShareMaxAgeHours { get; set; } = DefaultIncompleteShareMaxAgeHours;
}
