using SharingSystem.Domain.Constants;
using SharingSystem.Domain.Enums;

namespace SharingSystem.Domain.Entities;

/// <summary>
/// Runtime configuration that administrators can change from the admin panel.
/// The table always contains a single row.
/// </summary>
public class AppSettingsEntity
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    // General
    public string AppName { get; set; } = AppSettingsDefaults.AppName;

    /// <summary>Public base URL used in links and e-mails. Empty means "detect from the request".</summary>
    public string? AppUrl { get; set; }

    public bool ShowHomePage { get; set; } = true;

    public int SessionDurationHours { get; set; } = AppSettingsDefaults.SessionDurationHours;

    // Share
    public bool AllowRegistration { get; set; } = true;

    public bool AllowUnauthenticatedShares { get; set; }

    public long MaxShareSizeBytes { get; set; } = AppSettingsDefaults.MaxShareSizeBytes;

    /// <summary>Maximum share lifetime in hours. Zero means unlimited.</summary>
    public int MaxExpirationHours { get; set; }

    public ZipCompression ZipCompression { get; set; } = ZipCompression.Fastest;

    public int ChunkSizeBytes { get; set; } = AppSettingsDefaults.ChunkSizeBytes;

    // E-mail templates
    public bool EnableShareEmailRecipients { get; set; }

    public string ShareRecipientsSubject { get; set; } = AppSettingsDefaults.ShareRecipientsSubject;

    public string ShareRecipientsMessage { get; set; } = AppSettingsDefaults.ShareRecipientsMessage;

    public string ReverseShareSubject { get; set; } = AppSettingsDefaults.ReverseShareSubject;

    public string ReverseShareMessage { get; set; } = AppSettingsDefaults.ReverseShareMessage;

    public string ResetPasswordSubject { get; set; } = AppSettingsDefaults.ResetPasswordSubject;

    public string ResetPasswordMessage { get; set; } = AppSettingsDefaults.ResetPasswordMessage;

    public string InviteSubject { get; set; } = AppSettingsDefaults.InviteSubject;

    public string InviteMessage { get; set; } = AppSettingsDefaults.InviteMessage;

    // SMTP
    public bool SmtpEnabled { get; set; }

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = AppSettingsDefaults.SmtpPort;

    public string? SmtpFromEmail { get; set; }

    public string? SmtpUsername { get; set; }

    /// <summary>SMTP password, stored encrypted.</summary>
    public string? SmtpPasswordEncrypted { get; set; }

    public bool SmtpAllowUnauthorizedCertificates { get; set; }

    // Legal
    public bool LegalEnabled { get; set; }

    public string? ImprintText { get; set; }

    public string? PrivacyPolicyText { get; set; }

    public DateTime UpdatedAt { get; set; }
}
