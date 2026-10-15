using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SharingSystem.Application.Abstractions;

namespace SharingSystem.Infrastructure.Security;

/// <summary>
/// AES-256-GCM encryption for secrets persisted in the database.
/// Output format: Base64(nonce | tag | cipher text).
/// </summary>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmSecretProtector(IOptions<SecurityOptions> options)
    {
        if (!options.Value.TryGetKeyBytes(out _key))
        {
            throw new InvalidOperationException("The encryption key is not configured correctly.");
        }
    }

    public string Protect(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var output = new byte[NonceSize + TagSize + plainBytes.Length];
        var nonce = output.AsSpan(0, NonceSize);
        var tag = output.AsSpan(NonceSize, TagSize);
        var cipher = output.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);
        return Convert.ToBase64String(output);
    }

    public string Unprotect(string protectedText)
    {
        var input = Convert.FromBase64String(protectedText);
        if (input.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("The protected value is invalid.");
        }

        var nonce = input.AsSpan(0, NonceSize);
        var tag = input.AsSpan(NonceSize, TagSize);
        var cipher = input.AsSpan(NonceSize + TagSize);
        var plainBytes = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plainBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
