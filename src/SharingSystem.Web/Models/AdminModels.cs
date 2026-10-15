using System.ComponentModel.DataAnnotations;
using SharingSystem.Domain.Constants;
using SharingSystem.Domain.Enums;

namespace SharingSystem.Web.Models;

public sealed class CreateUserForm
{
    [Required]
    [StringLength(DomainLimits.UsernameMaxLength, MinimumLength = DomainLimits.UsernameMinLength)]
    [RegularExpression(DomainLimits.UsernamePattern, ErrorMessage = "Only letters, digits, dots, dashes and underscores are allowed.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(DomainLimits.PasswordMaxLength, MinimumLength = DomainLimits.PasswordMinLength)]
    [Display(Name = "Password (leave empty to send an invitation e-mail)")]
    public string? Password { get; set; }

    [Display(Name = "Administrator")]
    public bool IsAdmin { get; set; }
}

public sealed class EditUserForm
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(DomainLimits.UsernameMaxLength, MinimumLength = DomainLimits.UsernameMinLength)]
    [RegularExpression(DomainLimits.UsernamePattern, ErrorMessage = "Only letters, digits, dots, dashes and underscores are allowed.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Administrator")]
    public bool IsAdmin { get; set; }

    [DataType(DataType.Password)]
    [StringLength(DomainLimits.PasswordMaxLength, MinimumLength = DomainLimits.PasswordMinLength)]
    [Display(Name = "New password (optional)")]
    public string? NewPassword { get; set; }

    public bool TotpEnabled { get; set; }

    [Display(Name = "Disable two-factor authentication")]
    public bool DisableTotp { get; set; }
}

/// <summary>Admin settings form. Sizes are entered in megabytes.</summary>
public sealed class SettingsForm
{
    [Required]
    [StringLength(DomainLimits.AppNameMaxLength)]
    [Display(Name = "Application name")]
    public string AppName { get; set; } = string.Empty;

    [Url]
    [StringLength(DomainLimits.UrlMaxLength)]
    [Display(Name = "Application URL")]
    public string? AppUrl { get; set; }

    [Display(Name = "Show home page")]
    public bool ShowHomePage { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Session duration (hours)")]
    public int SessionDurationHours { get; set; }

    [Display(Name = "Allow registration")]
    public bool AllowRegistration { get; set; }

    [Display(Name = "Allow unauthenticated shares")]
    public bool AllowUnauthenticatedShares { get; set; }

    [Range(1, long.MaxValue)]
    [Display(Name = "Maximum share size (MB)")]
    public long MaxShareSizeMegabytes { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Maximum expiration (hours, 0 = unlimited)")]
    public int MaxExpirationHours { get; set; }

    [Display(Name = "Zip compression")]
    public ZipCompression ZipCompression { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Upload chunk size (MB)")]
    public int ChunkSizeMegabytes { get; set; }

    [Display(Name = "Allow sending shares to e-mail recipients")]
    public bool EnableShareEmailRecipients { get; set; }

    [Required, StringLength(DomainLimits.EmailSubjectMaxLength)]
    [Display(Name = "Share recipients subject")]
    public string ShareRecipientsSubject { get; set; } = string.Empty;

    [Required, StringLength(DomainLimits.EmailMessageMaxLength)]
    [Display(Name = "Share recipients message")]
    public string ShareRecipientsMessage { get; set; } = string.Empty;

    [Required, StringLength(DomainLimits.EmailSubjectMaxLength)]
    [Display(Name = "Reverse share subject")]
    public string ReverseShareSubject { get; set; } = string.Empty;

    [Required, StringLength(DomainLimits.EmailMessageMaxLength)]
    [Display(Name = "Reverse share message")]
    public string ReverseShareMessage { get; set; } = string.Empty;

    [Required, StringLength(DomainLimits.EmailSubjectMaxLength)]
    [Display(Name = "Reset password subject")]
    public string ResetPasswordSubject { get; set; } = string.Empty;

    [Required, StringLength(DomainLimits.EmailMessageMaxLength)]
    [Display(Name = "Reset password message")]
    public string ResetPasswordMessage { get; set; } = string.Empty;

    [Required, StringLength(DomainLimits.EmailSubjectMaxLength)]
    [Display(Name = "Invite subject")]
    public string InviteSubject { get; set; } = string.Empty;

    [Required, StringLength(DomainLimits.EmailMessageMaxLength)]
    [Display(Name = "Invite message")]
    public string InviteMessage { get; set; } = string.Empty;

    [Display(Name = "Enable SMTP")]
    public bool SmtpEnabled { get; set; }

    [StringLength(DomainLimits.SmtpHostMaxLength)]
    [Display(Name = "Host")]
    public string? SmtpHost { get; set; }

    [Range(1, ushort.MaxValue)]
    [Display(Name = "Port")]
    public int SmtpPort { get; set; }

    [EmailAddress]
    [Display(Name = "Sender e-mail")]
    public string? SmtpFromEmail { get; set; }

    [Display(Name = "Username")]
    public string? SmtpUsername { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string? NewSmtpPassword { get; set; }

    [Display(Name = "Remove stored password")]
    public bool ClearSmtpPassword { get; set; }

    public bool HasSmtpPassword { get; set; }

    [Display(Name = "Allow unauthorized (self-signed) certificates")]
    public bool SmtpAllowUnauthorizedCertificates { get; set; }

    [Display(Name = "Show imprint and privacy policy")]
    public bool LegalEnabled { get; set; }

    [StringLength(DomainLimits.LegalTextMaxLength)]
    [Display(Name = "Imprint")]
    public string? ImprintText { get; set; }

    [StringLength(DomainLimits.LegalTextMaxLength)]
    [Display(Name = "Privacy policy")]
    public string? PrivacyPolicyText { get; set; }
}

public sealed class TestEmailForm
{
    [Required]
    [EmailAddress]
    [Display(Name = "Send test e-mail to")]
    public string Email { get; set; } = string.Empty;
}
