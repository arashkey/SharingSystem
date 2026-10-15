using System.Text;
using SharingSystem.Domain.Constants;

namespace SharingSystem.Application.Common;

/// <summary>
/// Removes path segments and unsafe characters from user supplied file names.
/// </summary>
public static class FileNameSanitizer
{
    private const string FallbackName = "file";
    private static readonly char[] InvalidCharacters =
        [.. Path.GetInvalidFileNameChars(), '/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    public static string Sanitize(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return FallbackName;
        }

        var name = fileName.Replace('\\', '/');
        name = name[(name.LastIndexOf('/') + 1)..];

        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            builder.Append(char.IsControl(character) || InvalidCharacters.Contains(character) ? '_' : character);
        }

        var sanitized = builder.ToString().Trim().Trim('.');
        if (sanitized.Length == 0)
        {
            return FallbackName;
        }

        if (sanitized.Length <= DomainLimits.FileNameMaxLength)
        {
            return sanitized;
        }

        var extension = Path.GetExtension(sanitized);
        var baseLength = Math.Max(1, DomainLimits.FileNameMaxLength - extension.Length);
        return sanitized[..baseLength] + extension;
    }
}
