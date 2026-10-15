using System.ComponentModel.DataAnnotations;

namespace SharingSystem.Infrastructure.Storage;

/// <summary>
/// File system locations (bound from the "Storage" section).
/// </summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Root folder for uploads, archives and data protection keys. Relative paths are resolved against the content root.</summary>
    [Required]
    public string DataPath { get; set; } = "data";
}
