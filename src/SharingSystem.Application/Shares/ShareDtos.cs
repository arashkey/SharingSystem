using SharingSystem.Application.Common;
using SharingSystem.Domain.Enums;

namespace SharingSystem.Application.Shares;

/// <summary>Requested lifetime of a share or reverse share.</summary>
public sealed class ExpirationRequest
{
    public bool NeverExpires { get; init; }
    public int Value { get; init; } = 1;
    public ExpirationUnit Unit { get; init; } = ExpirationUnit.Days;
}

public sealed class CreateShareRequest
{
    /// <summary>Custom link identifier. A random identifier is generated when empty.</summary>
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Password { get; init; }
    public int? MaxViews { get; init; }
    public ExpirationRequest Expiration { get; init; } = new();
    public List<string> Recipients { get; init; } = [];

    /// <summary>Token of the reverse share that is used to upload, if any.</summary>
    public string? ReverseShareToken { get; init; }
}

public sealed record CreatedShareDto(string Id, string UploadToken);

public sealed class UploadChunkRequest
{
    public string ShareId { get; init; } = string.Empty;
    public string? UploadToken { get; init; }
    public Guid? FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public int ChunkIndex { get; init; }
    public int TotalChunks { get; init; }
    public required Stream Content { get; init; }
}

public sealed record UploadChunkResult(Guid FileId, bool IsFileCompleted);

public sealed record CompletedShareDto(string Id, string? Url, bool IsPublic);

public sealed class ShareFileDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public long Size { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

/// <summary>Share information used in lists.</summary>
public sealed class ShareSummaryDto
{
    public string Id { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int Views { get; init; }
    public int? MaxViews { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsCompleted { get; init; }
    public bool HasPassword { get; init; }
    public bool IsCreatorOnly { get; init; }
    public int FileCount { get; init; }
    public long TotalSize { get; init; }
    public string? CreatorUsername { get; init; }
}

/// <summary>Share information including one page of its files.</summary>
public sealed class ShareDetailsDto
{
    public string Id { get; init; } = string.Empty;
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int Views { get; init; }
    public int? MaxViews { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool HasPassword { get; init; }
    public bool IsCreatorOnly { get; init; }
    public Guid? CreatorId { get; init; }
    public string? CreatorUsername { get; init; }
    public PagedResult<ShareFileDto> Files { get; set; } =
        new([], Pagination.FirstPage, Pagination.DefaultPageSize, 0);

    /// <summary>Number of all files of the share, not only of the current page.</summary>
    public int FileCount => Files.TotalCount;

    /// <summary>Size of all files of the share, not only of the current page.</summary>
    public long TotalSize { get; set; }
}

/// <summary>A share as seen by a visitor of the public share page.</summary>
public sealed class ShareViewDto
{
    public required ShareDetailsDto Share { get; init; }

    /// <summary>The visitor is the owner or an administrator.</summary>
    public bool CanManage { get; init; }
}

public sealed class UpdateShareRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public int? MaxViews { get; init; }
    public bool ChangeExpiration { get; init; }
    public ExpirationRequest Expiration { get; init; } = new();
    public string? NewPassword { get; init; }
    public bool RemovePassword { get; init; }
}

public sealed record FileContentDto(Stream Content, string FileName, string ContentType);

/// <summary>Limits that the upload page needs to know.</summary>
public sealed record UploadLimitsDto(long MaxShareSizeBytes, int ChunkSizeBytes, int MaxExpirationHours);
