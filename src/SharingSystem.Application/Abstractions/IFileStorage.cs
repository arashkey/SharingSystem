using SharingSystem.Domain.Enums;

namespace SharingSystem.Application.Abstractions;

/// <summary>
/// Stores the binary content of share files.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="bytes"/> more bytes can be stored without using the free disk space
    /// that is kept in reserve for the database and the operating system.
    /// </summary>
    Task<bool> HasFreeSpaceForAsync(long bytes, CancellationToken cancellationToken);

    /// <summary>Returns the number of bytes already received for a file that is still being uploaded.</summary>
    Task<long> GetPartialLengthAsync(string shareId, Guid fileId, CancellationToken cancellationToken);

    /// <summary>
    /// Appends a chunk to a partially uploaded file. Writes at most <paramref name="maxBytes"/> bytes;
    /// when the content is larger, the chunk is rolled back and <see cref="ChunkWriteResult.LimitExceeded"/> is set.
    /// </summary>
    Task<ChunkWriteResult> AppendChunkAsync(
        string shareId, Guid fileId, Stream content, long maxBytes, CancellationToken cancellationToken);

    /// <summary>Marks a partial file as complete and returns its final size.</summary>
    Task<long> CompleteFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken);

    Task DeletePartialFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string shareId, Guid fileId, CancellationToken cancellationToken);

    Task DeleteFileAsync(string shareId, Guid fileId, CancellationToken cancellationToken);

    Task DeleteShareAsync(string shareId, CancellationToken cancellationToken);

    /// <summary>
    /// Opens (and lazily builds) a zip archive with all files of a share.
    /// Throws an <see cref="Common.AppException"/> when there is not enough free disk space to build it.
    /// </summary>
    Task<Stream> OpenArchiveAsync(
        string shareId, IReadOnlyCollection<ArchiveEntry> entries, ZipCompression compression, CancellationToken cancellationToken);

    Task DeleteArchiveAsync(string shareId, CancellationToken cancellationToken);

    /// <summary>Deletes partial uploads that were not touched for the given time span.</summary>
    Task<int> DeleteStalePartialFilesAsync(TimeSpan olderThan, CancellationToken cancellationToken);
}

public sealed record ChunkWriteResult(long BytesWritten, bool LimitExceeded);

public sealed record ArchiveEntry(Guid FileId, string Name);
