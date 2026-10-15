namespace SharingSystem.Domain.Entities;

/// <summary>
/// A one-time password reset token. Only the hash of the token is stored.
/// </summary>
public class PasswordResetTokenEntity
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public UserEntity? User { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
