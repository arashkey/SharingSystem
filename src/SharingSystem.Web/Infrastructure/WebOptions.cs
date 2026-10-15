using System.ComponentModel.DataAnnotations;

namespace SharingSystem.Web.Infrastructure;

/// <summary>
/// Rate limiting of authentication endpoints (bound from the "RateLimiting" section).
/// </summary>
public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    private const int DefaultPermitLimit = 10;
    private const int DefaultWindowSeconds = 60;
    private const int MaxPermitLimit = 10_000;
    private const int MaxWindowSeconds = 3600;

    [Range(1, MaxPermitLimit)]
    public int AuthPermitLimit { get; set; } = DefaultPermitLimit;

    [Range(1, MaxWindowSeconds)]
    public int AuthWindowSeconds { get; set; } = DefaultWindowSeconds;
}

/// <summary>
/// Lifetimes of short-lived cookies (bound from the "Cookies" section).
/// </summary>
public sealed class CookieLifetimeOptions
{
    public const string SectionName = "Cookies";

    private const int DefaultTwoFactorMinutes = 5;
    private const int DefaultShareAccessHours = 12;
    private const int MaxMinutes = 60;
    private const int MaxHours = 24 * 30;

    [Range(1, MaxMinutes)]
    public int TwoFactorMinutes { get; set; } = DefaultTwoFactorMinutes;

    /// <summary>How long a visitor keeps access to a share after entering the password / visiting it.</summary>
    [Range(1, MaxHours)]
    public int ShareAccessHours { get; set; } = DefaultShareAccessHours;
}
