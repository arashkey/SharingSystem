namespace SharingSystem.Domain.Entities;

/// <summary>
/// A reverse share is a link that allows other people (even without an account)
/// to upload files and create a share owned by the reverse share creator.
/// </summary>
public class ReverseShareEntity
{
    public Guid Id { get; set; }

    /// <summary>Public link token.</summary>
    public string Token { get; set; } = string.Empty;

    public Guid CreatorId { get; set; }

    public UserEntity? Creator { get; set; }

    /// <summary>The reverse share link and the shares created with it expire at this date (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    public long MaxShareSizeBytes { get; set; }

    public int RemainingUses { get; set; }

    public bool SendEmailNotification { get; set; }

    /// <summary>Simplified mode hides all share options from the uploader.</summary>
    public bool IsSimplified { get; set; }

    /// <summary>When disabled, only the creator can view the shares created with this link.</summary>
    public bool IsPublicAccess { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ShareEntity> Shares { get; set; } = new List<ShareEntity>();
}
