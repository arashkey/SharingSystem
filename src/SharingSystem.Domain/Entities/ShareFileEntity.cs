namespace SharingSystem.Domain.Entities;

/// <summary>
/// Metadata of a single file that belongs to a share. The content lives in file storage.
/// </summary>
public class ShareFileEntity
{
    public Guid Id { get; set; }

    public string ShareId { get; set; } = string.Empty;

    public ShareEntity? Share { get; set; }

    public string Name { get; set; } = string.Empty;

    public long Size { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
