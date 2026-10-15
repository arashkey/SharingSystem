using System.ComponentModel.DataAnnotations;

namespace SharingSystem.Infrastructure.Storage;

/// <summary>
/// File system locations and storage safety limits (bound from the "Storage" section).
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    private const long BytesPerMegabyte = 1024L * 1024L;
    private const long DefaultMinFreeDiskSpaceBytes = 1024L * BytesPerMegabyte;
    private const int DefaultMaxConcurrentArchiveBuilds = 2;
    private const int MaxConcurrentArchiveBuildsLimit = 64;

    /// <summary>Root folder for uploads, archives and data protection keys. Relative paths are resolved against the content root.</summary>
    [Required]
    public string DataPath { get; set; } = "data";

    /// <summary>
    /// Free disk space that is always kept in reserve. Uploads and zip archives that would use this space are rejected,
    /// so a full disk can never break the database or the operating system. <c>0</c> disables the check.
    /// </summary>
    [Range(0, long.MaxValue)]
    public long MinFreeDiskSpaceBytes { get; set; } = DefaultMinFreeDiskSpaceBytes;

    /// <summary>Maximum number of "download all" zip archives that are built at the same time.</summary>
    [Range(1, MaxConcurrentArchiveBuildsLimit)]
    public int MaxConcurrentArchiveBuilds { get; set; } = DefaultMaxConcurrentArchiveBuilds;
}
