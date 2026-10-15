using System.Collections.Concurrent;
using System.IO.Compression;
using Microsoft.Extensions.Logging;
using SharingSystem.Application.Abstractions;
using SharingSystem.Domain.Enums;

namespace SharingSystem.Infrastructure.Storage;

/// <summary>
/// Stores share files on the local file system:
/// <c>uploads/{shareId}/{fileId}</c> for files and <c>archives/{shareId}.zip</c> for "download all" archives.
/// </summary>
public sealed class LocalFileStorage : IFileStorage
{
    private const string UploadsFolder = "uploads";
    private const string ArchivesFolder = "archives";
    private const string PartialExtension = ".part";
    private const string ArchiveExtension = ".zip";
    private const string TemporaryExtension = ".tmp";
    private const int BufferSize = 81920;

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ArchiveLocks = new();

    private readonly string _uploadsRoot;
    private readonly string _archivesRoot;
    private readonly ILogger<LocalFileStorage> _logger;

    public LocalFileStorage(DataPathProvider dataPathProvider, ILogger<LocalFileStorage> logger)
    {
        _uploadsRoot = Path.Combine(dataPathProvider.DataPath, UploadsFolder);
        _archivesRoot = Path.Combine(dataPathProvider.DataPath, ArchivesFolder);
        _logger = logger;
        Directory.CreateDirectory(_uploadsRoot);
        Directory.CreateDirectory(_archivesRoot);
    }

    public Task<long> GetPartialLengthAsync(string shareId, Guid fileId, CancellationToken cancellationToken)
    {
        var info = new FileInfo(GetPartialPath(shareId, fileId));
        return Task.FromResult(info.Exists ? info.Length : 0L);
    }

    public async Task<ChunkWriteResult> AppendChunkAsync(
        string shareId, Guid fileId, Stream content, long maxBytes, CancellationToken cancellationToken)
    {
        var path = GetPartialPath(shareId, fileId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var target = new FileStream(
            path, FileMode.Append, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
        var originalLength = target.Length;
        var buffer = new byte[BufferSize];
        long written = 0;

        try
        {
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                written += read;
                if (written > maxBytes)
                {
                    target.SetLength(originalLength);
                    return new ChunkWriteResult(0, true);
                }

                await target.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        catch
        {
            // Roll back incomplete chunks (e.g. aborted requests) so that the client can retry.
            target.SetLength(originalLength);
            throw;
        }

        return new ChunkWriteResult(written, false);
    }

    public Task<long> CompleteFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken)
    {
        var partialPath = GetPartialPath(shareId, fileId);
        var finalPath = GetFilePath(shareId, fileId);

        if (!File.Exists(partialPath))
        {
            // Empty files never create a partial file.
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
            File.WriteAllBytes(finalPath, []);
            return Task.FromResult(0L);
        }

        File.Move(partialPath, finalPath, overwrite: true);
        return Task.FromResult(new FileInfo(finalPath).Length);
    }

    public Task DeletePartialFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken)
    {
        TryDelete(GetPartialPath(shareId, fileId));
        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string shareId, Guid fileId, CancellationToken cancellationToken)
    {
        var path = GetFilePath(shareId, fileId);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The stored file is missing.", path);
        }

        return Task.FromResult<Stream>(OpenForReading(path));
    }

    public Task DeleteFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken)
    {
        TryDelete(GetFilePath(shareId, fileId));
        return Task.CompletedTask;
    }

    public Task DeleteShareAsync(string shareId, CancellationToken cancellationToken)
    {
        var directory = GetShareDirectory(shareId);
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Could not delete the folder of share {ShareId}", shareId);
        }

        TryDelete(GetArchivePath(shareId));
        return Task.CompletedTask;
    }

    public async Task<Stream> OpenArchiveAsync(
        string shareId, IReadOnlyCollection<ArchiveEntry> entries, ZipCompression compression, CancellationToken cancellationToken)
    {
        var archivePath = GetArchivePath(shareId);
        var archiveLock = ArchiveLocks.GetOrAdd(shareId, _ => new SemaphoreSlim(1, 1));

        await archiveLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(archivePath))
            {
                await Task.Run(() => BuildArchive(shareId, entries, compression, archivePath), cancellationToken);
            }

            return OpenForReading(archivePath);
        }
        finally
        {
            archiveLock.Release();
        }
    }

    public Task DeleteArchiveAsync(string shareId, CancellationToken cancellationToken)
    {
        TryDelete(GetArchivePath(shareId));
        return Task.CompletedTask;
    }

    public Task<int> DeleteStalePartialFilesAsync(TimeSpan olderThan, CancellationToken cancellationToken)
    {
        var threshold = DateTime.UtcNow - olderThan;
        var deleted = 0;

        foreach (var file in Directory.EnumerateFiles(_uploadsRoot, "*" + PartialExtension, SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.GetLastWriteTimeUtc(file) < threshold && TryDelete(file))
            {
                deleted++;
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(_uploadsRoot))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }

        return Task.FromResult(deleted);
    }

    private void BuildArchive(string shareId, IReadOnlyCollection<ArchiveEntry> entries, ZipCompression compression, string archivePath)
    {
        var temporaryPath = archivePath + "." + Guid.NewGuid().ToString("N") + TemporaryExtension;
        var level = compression switch
        {
            ZipCompression.NoCompression => CompressionLevel.NoCompression,
            ZipCompression.Optimal => CompressionLevel.Optimal,
            ZipCompression.SmallestSize => CompressionLevel.SmallestSize,
            _ => CompressionLevel.Fastest
        };

        try
        {
            using (var archive = ZipFile.Open(temporaryPath, ZipArchiveMode.Create))
            {
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in entries)
                {
                    archive.CreateEntryFromFile(GetFilePath(shareId, entry.FileId), GetUniqueName(entry.Name, usedNames), level);
                }
            }

            File.Move(temporaryPath, archivePath, overwrite: true);
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }
    }

    private static string GetUniqueName(string name, HashSet<string> usedNames)
    {
        var candidate = name;
        var extension = Path.GetExtension(name);
        var baseName = Path.GetFileNameWithoutExtension(name);
        var counter = 1;

        while (!usedNames.Add(candidate))
        {
            candidate = $"{baseName} ({counter++}){extension}";
        }

        return candidate;
    }

    private static FileStream OpenForReading(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, BufferSize, FileOptions.Asynchronous);

    private string GetShareDirectory(string shareId) => EnsureInside(_uploadsRoot, Path.Combine(_uploadsRoot, shareId));

    private string GetFilePath(string shareId, Guid fileId) =>
        Path.Combine(GetShareDirectory(shareId), fileId.ToString("N"));

    private string GetPartialPath(string shareId, Guid fileId) => GetFilePath(shareId, fileId) + PartialExtension;

    private string GetArchivePath(string shareId) =>
        EnsureInside(_archivesRoot, Path.Combine(_archivesRoot, shareId + ArchiveExtension));

    private static string EnsureInside(string root, string path)
    {
        var fullRoot = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(fullRoot, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return fullPath;
    }

    private bool TryDelete(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Could not delete {Path}", path);
            return false;
        }
    }
}
