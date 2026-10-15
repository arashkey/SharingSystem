namespace SharingSystem.Application.Abstractions;

/// <summary>One-way password hashing.</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string hash, string password);
}

/// <summary>Symmetric encryption for secrets that are persisted in the database.</summary>
public interface ISecretProtector
{
    string Protect(string plainText);

    string Unprotect(string protectedText);
}

/// <summary>Time-based one-time passwords (RFC 6238) for two-factor authentication.</summary>
public interface ITotpService
{
    string GenerateSecret();

    string BuildProvisioningUri(string issuer, string accountName, string secret);

    /// <summary>Renders the provisioning URI as a PNG QR code data URI.</summary>
    string GenerateQrCodeDataUri(string provisioningUri);

    bool VerifyCode(string secret, string code);
}
