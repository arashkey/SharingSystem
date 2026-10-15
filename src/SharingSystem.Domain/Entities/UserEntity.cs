namespace SharingSystem.Domain.Entities;

/// <summary>
/// An application user that can sign in, create shares and reverse shares.
/// </summary>
public class UserEntity
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>One-way password hash (never the plain password).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }

    /// <summary>
    /// Random value that changes whenever security-relevant data changes
    /// (password, role, 2FA). Used to invalidate existing sessions.
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>TOTP secret, stored encrypted.</summary>
    public string? TotpSecretEncrypted { get; set; }

    public bool TotpEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ShareEntity> Shares { get; set; } = new List<ShareEntity>();

    public ICollection<ReverseShareEntity> ReverseShares { get; set; } = new List<ReverseShareEntity>();
}
