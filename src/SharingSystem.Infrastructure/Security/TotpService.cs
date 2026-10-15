using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using QRCoder;
using SharingSystem.Application.Abstractions;
using SharingSystem.Domain.Constants;

namespace SharingSystem.Infrastructure.Security;

/// <summary>
/// RFC 6238 TOTP (SHA-1, 6 digits, 30 second steps) compatible with common authenticator apps.
/// </summary>
public sealed class TotpService(TimeProvider timeProvider) : ITotpService
{
    private const int SecretSizeBytes = 20;
    private const int TimeStepSeconds = 30;
    private const int AllowedDriftSteps = 1;
    private const int QrPixelsPerModule = 6;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int BitsPerBase32Char = 5;
    private const int BitsPerByte = 8;
    private const int DynamicTruncationMask = 0x0F;
    private const int SignBitMask = 0x7FFFFFFF;

    private static readonly int CodeModulo = (int)Math.Pow(10, DomainLimits.TotpCodeLength);

    public string GenerateSecret() => Base32Encode(RandomNumberGenerator.GetBytes(SecretSizeBytes));

    public string BuildProvisioningUri(string issuer, string accountName, string secret)
    {
        var label = Uri.EscapeDataString($"{issuer}:{accountName}");
        return $"otpauth://totp/{label}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}" +
               $"&digits={DomainLimits.TotpCodeLength}&period={TimeStepSeconds}";
    }

    public string GenerateQrCodeDataUri(string provisioningUri)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(provisioningUri, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(data).GetGraphic(QrPixelsPerModule);
        return "data:image/png;base64," + Convert.ToBase64String(png);
    }

    public bool VerifyCode(string secret, string code)
    {
        var normalized = code.Replace(" ", string.Empty, StringComparison.Ordinal);
        if (normalized.Length != DomainLimits.TotpCodeLength || !normalized.All(char.IsAsciiDigit))
        {
            return false;
        }

        var key = Base32Decode(secret);
        var currentStep = timeProvider.GetUtcNow().ToUnixTimeSeconds() / TimeStepSeconds;

        for (var offset = -AllowedDriftSteps; offset <= AllowedDriftSteps; offset++)
        {
            var expected = ComputeCode(key, currentStep + offset);
            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(normalized)))
            {
                return true;
            }
        }

        return false;
    }

    private static string ComputeCode(byte[] key, long step)
    {
        Span<byte> counter = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);

        var hash = HMACSHA1.HashData(key, counter);
        var offset = hash[^1] & DynamicTruncationMask;
        var binary = BinaryPrimitives.ReadInt32BigEndian(hash.AsSpan(offset, sizeof(int))) & SignBitMask;
        return (binary % CodeModulo).ToString(CultureInfo.InvariantCulture).PadLeft(DomainLimits.TotpCodeLength, '0');
    }

    private static string Base32Encode(byte[] data)
    {
        var builder = new StringBuilder();
        int buffer = 0, bitsLeft = 0;

        foreach (var value in data)
        {
            buffer = (buffer << BitsPerByte) | value;
            bitsLeft += BitsPerByte;
            while (bitsLeft >= BitsPerBase32Char)
            {
                builder.Append(Base32Alphabet[(buffer >> (bitsLeft - BitsPerBase32Char)) & (Base32Alphabet.Length - 1)]);
                bitsLeft -= BitsPerBase32Char;
            }
        }

        if (bitsLeft > 0)
        {
            builder.Append(Base32Alphabet[(buffer << (BitsPerBase32Char - bitsLeft)) & (Base32Alphabet.Length - 1)]);
        }

        return builder.ToString();
    }

    private static byte[] Base32Decode(string input)
    {
        var cleaned = input.TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>(cleaned.Length * BitsPerBase32Char / BitsPerByte);
        int buffer = 0, bitsLeft = 0;

        foreach (var character in cleaned)
        {
            var value = Base32Alphabet.IndexOf(character);
            if (value < 0)
            {
                throw new FormatException("The TOTP secret is not valid Base32.");
            }

            buffer = (buffer << BitsPerBase32Char) | value;
            bitsLeft += BitsPerBase32Char;
            if (bitsLeft >= BitsPerByte)
            {
                output.Add((byte)(buffer >> (bitsLeft - BitsPerByte)));
                bitsLeft -= BitsPerByte;
            }
        }

        return [.. output];
    }
}
