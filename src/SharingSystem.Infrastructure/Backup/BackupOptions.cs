using System.ComponentModel.DataAnnotations;

namespace SharingSystem.Infrastructure.Backup;

/// <summary>
/// Settings of the periodic database snapshots (bound from the "Backup" section).
/// </summary>
public sealed class BackupOptions
{
    public const string SectionName = "Backup";

    private const int DefaultIntervalHours = 6;
    private const int DefaultRetentionCount = 28;
    private const int MaxIntervalHours = 7 * 24;
    private const int MaxRetentionCount = 10_000;

    /// <summary>Enables the background job that writes database snapshots.</summary>
    public bool Enabled { get; set; } = true;

    [Range(1, MaxIntervalHours)]
    public int IntervalHours { get; set; } = DefaultIntervalHours;

    /// <summary>Number of snapshots that are kept; older snapshots are deleted.</summary>
    [Range(1, MaxRetentionCount)]
    public int RetentionCount { get; set; } = DefaultRetentionCount;

    /// <summary>Folder of the snapshots. Relative paths are resolved against the data folder.</summary>
    [Required]
    public string Path { get; set; } = "backups";
}
