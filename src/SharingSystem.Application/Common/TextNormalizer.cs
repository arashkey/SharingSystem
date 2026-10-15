namespace SharingSystem.Application.Common;

public static class TextNormalizer
{
    /// <summary>Trims an optional text, converts blank values to <c>null</c> and cuts it to the maximum length.</summary>
    public static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
