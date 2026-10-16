using System.Reflection;

namespace SharingSystem.Web.Infrastructure;

/// <summary>
/// Release version of the application, taken from the &lt;Version&gt; property of the web project.
/// </summary>
public static class AppVersion
{
    private const char BuildMetadataSeparator = '+';

    public static string Current { get; } = Resolve();

    private static string Resolve()
    {
        var assembly = typeof(AppVersion).Assembly;
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return assembly.GetName().Version?.ToString() ?? string.Empty;
        }

        // The SDK appends "+<commit hash>" as build metadata; only the release version is shown.
        var separatorIndex = informationalVersion.IndexOf(BuildMetadataSeparator);
        return separatorIndex < 0 ? informationalVersion : informationalVersion[..separatorIndex];
    }
}
