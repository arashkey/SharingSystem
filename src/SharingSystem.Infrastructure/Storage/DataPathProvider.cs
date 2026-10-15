namespace SharingSystem.Infrastructure.Storage;

/// <summary>
/// Provides the absolute path of the data folder.
/// </summary>
public sealed class DataPathProvider
{
    public DataPathProvider(string contentRootPath, StorageOptions options)
    {
        DataPath = Path.GetFullPath(Path.IsPathRooted(options.DataPath)
            ? options.DataPath
            : Path.Combine(contentRootPath, options.DataPath));
        Directory.CreateDirectory(DataPath);
    }

    public string DataPath { get; }
}
