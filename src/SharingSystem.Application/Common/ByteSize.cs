using System.Globalization;

namespace SharingSystem.Application.Common;

/// <summary>
/// Formats byte counts into human readable strings.
/// </summary>
public static class ByteSize
{
    public const long BytesPerKilobyte = 1024;
    public const long BytesPerMegabyte = BytesPerKilobyte * 1024;

    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Format(long bytes)
    {
        double value = bytes;
        var unitIndex = 0;

        while (value >= BytesPerKilobyte && unitIndex < Units.Length - 1)
        {
            value /= BytesPerKilobyte;
            unitIndex++;
        }

        var format = unitIndex == 0 ? "0" : "0.##";
        return $"{value.ToString(format, CultureInfo.InvariantCulture)} {Units[unitIndex]}";
    }
}
