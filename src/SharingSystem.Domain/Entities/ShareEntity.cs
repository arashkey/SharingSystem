namespace SharingSystem.Domain.Entities;

/// <summary>
/// A share is a collection of uploaded files reachable through a public link.
/// </summary>
public class ShareEntity
{
    /// <summary>The share identifier, also used as the public link slug.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? Description { get; set; }

    /// <summary>Optional one-way hash of the share password.</summary>
    public string? PasswordHash { get; set; }

    public int? MaxViews { get; set; }

    public int Views { get; set; }

    /// <summary>Expiration date in UTC; <c>null</c> means the share never expires.</summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>A share becomes visible only after the uploader completed it.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Hash of the token that authorizes uploads while the share is still a draft.</summary>
    public string? UploadTokenHash { get; set; }

    /// <summary>Comma separated list of e-mail addresses that were notified.</summary>
    public string? RecipientEmails { get; set; }

    public Guid? CreatorId { get; set; }

    public UserEntity? Creator { get; set; }

    /// <summary>
    /// When <c>true</c>, only the creator (and administrators) can open the share.
    /// Set for shares that were uploaded through a reverse share without public access.
    /// </summary>
    public bool IsCreatorOnly { get; set; }

    public Guid? ReverseShareId { get; set; }

    public ReverseShareEntity? ReverseShare { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ShareFileEntity> Files { get; set; } = new List<ShareFileEntity>();
}
