using SharingSystem.Domain.Enums;

namespace SharingSystem.Application.Settings;

/// <summary>
/// Runtime configuration as exposed to the application. Secrets are never included.
/// </summary>
public sealed class AppSettingsDto
{
    public string AppName { get; init; } = string.Empty;
    public string? AppUrl { get; init; }
    public bool ShowHomePage { get; init; }
    public int SessionDurationHours { get; init; }

    public bool AllowRegistration { get; init; }
    public bool AllowUnauthenticatedShares { get; init; }
    public long MaxShareSizeBytes { get; init; }
    public int MaxExpirationHours { get; init; }
    public ZipCompression ZipCompression { get; init; }
    public int ChunkSizeBytes { get; init; }

    public bool EnableShareEmailRecipients { get; init; }
    public string ShareRecipientsSubject { get; init; } = string.Empty;
    public string ShareRecipientsMessage { get; init; } = string.Empty;
    public string ReverseShareSubject { get; init; } = string.Empty;
    public string ReverseShareMessage { get; init; } = string.Empty;
    public string ResetPasswordSubject { get; init; } = string.Empty;
    public string ResetPasswordMessage { get; init; } = string.Empty;
    public string InviteSubject { get; init; } = string.Empty;
    public string InviteMessage { get; init; } = string.Empty;

    public bool SmtpEnabled { get; init; }
    public string? SmtpHost { get; init; }
    public int SmtpPort { get; init; }
    public string? SmtpFromEmail { get; init; }
    public string? SmtpUsername { get; init; }
    public bool HasSmtpPassword { get; init; }
    public bool SmtpAllowUnauthorizedCertificates { get; init; }

    public bool LegalEnabled { get; init; }
    public string? ImprintText { get; init; }
    public string? PrivacyPolicyText { get; init; }

    /// <summary>Whether e-mails can be sent at all.</summary>
    public bool CanSendEmails => SmtpEnabled && !string.IsNullOrWhiteSpace(SmtpHost);
}

/// <summary>
/// Changes to the runtime configuration made by an administrator.
/// </summary>
public sealed class UpdateAppSettingsRequest
{
    public string AppName { get; init; } = string.Empty;
    public string? AppUrl { get; init; }
    public bool ShowHomePage { get; init; }
    public int SessionDurationHours { get; init; }

    public bool AllowRegistration { get; init; }
    public bool AllowUnauthenticatedShares { get; init; }
    public long MaxShareSizeBytes { get; init; }
    public int MaxExpirationHours { get; init; }
    public ZipCompression ZipCompression { get; init; }
    public int ChunkSizeBytes { get; init; }

    public bool EnableShareEmailRecipients { get; init; }
    public string ShareRecipientsSubject { get; init; } = string.Empty;
    public string ShareRecipientsMessage { get; init; } = string.Empty;
    public string ReverseShareSubject { get; init; } = string.Empty;
    public string ReverseShareMessage { get; init; } = string.Empty;
    public string ResetPasswordSubject { get; init; } = string.Empty;
    public string ResetPasswordMessage { get; init; } = string.Empty;
    public string InviteSubject { get; init; } = string.Empty;
    public string InviteMessage { get; init; } = string.Empty;

    public bool SmtpEnabled { get; init; }
    public string? SmtpHost { get; init; }
    public int SmtpPort { get; init; }
    public string? SmtpFromEmail { get; init; }
    public string? SmtpUsername { get; init; }
    public bool SmtpAllowUnauthorizedCertificates { get; init; }

    /// <summary>New SMTP password. Empty keeps the stored password.</summary>
    public string? NewSmtpPassword { get; init; }

    public bool ClearSmtpPassword { get; init; }

    public bool LegalEnabled { get; init; }
    public string? ImprintText { get; init; }
    public string? PrivacyPolicyText { get; init; }
}

/// <summary>
/// SMTP connection data including the decrypted password. Never expose this outside of the e-mail sender.
/// </summary>
public sealed record SmtpConfiguration(
    bool Enabled,
    string? Host,
    int Port,
    string? FromEmail,
    string? Username,
    string? Password,
    bool AllowUnauthorizedCertificates,
    string AppName);
