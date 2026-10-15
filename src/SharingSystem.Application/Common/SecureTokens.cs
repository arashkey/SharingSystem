using System.Security.Cryptography;
using System.Text;

namespace SharingSystem.Application.Common;

/// <summary>
/// Helpers for generating and hashing random tokens.
/// </summary>
public static class SecureTokens
{
    private const int DefaultTokenBytes = 32;
    private const string AlphaNumeric = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <summary>Creates a URL-safe random token.</summary>
    public static string CreateUrlSafeToken(int byteCount = DefaultTokenBytes)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteCount);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Creates a random alphanumeric string, e.g. for share identifiers.</summary>
    public static string CreateAlphaNumeric(int length) =>
        RandomNumberGenerator.GetString(AlphaNumeric, length);

    /// <summary>SHA-256 hash (hex) of a high-entropy token. Not suitable for user passwords.</summary>
    public static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Constant-time comparison of a token with a stored hash.</summary>
    public static bool Matches(string? token, string? expectedHash)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expectedHash))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(token)),
            Encoding.ASCII.GetBytes(expectedHash));
    }
}
