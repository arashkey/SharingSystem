using System.ComponentModel.DataAnnotations;

namespace SharingSystem.Infrastructure.Security;

/// <summary>
/// Security related configuration (bound from the "Security" section).
/// </summary>
public sealed class SecurityOptions : IValidatableObject
{
    public const string SectionName = "Security";
    public const int EncryptionKeySizeBytes = 32;

    /// <summary>
    /// Base64 encoded 256-bit key used to encrypt secrets stored in the database (SMTP password, TOTP secrets).
    /// Must be provided through environment variables or user secrets, never committed to source control.
    /// </summary>
    public string EncryptionKey { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TryGetKeyBytes(out _))
        {
            yield return new ValidationResult(
                $"{SectionName}:EncryptionKey must be a Base64 encoded {EncryptionKeySizeBytes}-byte key. " +
                "Set it with the environment variable Security__EncryptionKey or with 'dotnet user-secrets'.",
                [nameof(EncryptionKey)]);
        }
    }

    public bool TryGetKeyBytes(out byte[] key)
    {
        key = [];
        if (string.IsNullOrWhiteSpace(EncryptionKey))
        {
            return false;
        }

        try
        {
            key = Convert.FromBase64String(EncryptionKey.Trim());
            return key.Length == EncryptionKeySizeBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
