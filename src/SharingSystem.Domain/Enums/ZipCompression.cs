namespace SharingSystem.Domain.Enums;

/// <summary>
/// Compression level used when building the "download all" archive of a share.
/// </summary>
public enum ZipCompression
{
    NoCompression = 0,
    Fastest = 1,
    Optimal = 2,
    SmallestSize = 3
}
