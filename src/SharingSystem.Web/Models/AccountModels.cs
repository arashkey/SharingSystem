using System.ComponentModel.DataAnnotations;
using SharingSystem.Application.Account;
using SharingSystem.Domain.Constants;

namespace SharingSystem.Web.Models;

public sealed class AccountPageViewModel
{
    public required AccountDto Account { get; init; }
    public required ProfileForm Profile { get; init; }
    public TotpSetupDto? TotpSetup { get; init; }
}

public sealed class ProfileForm
{
    [Required]
    [StringLength(DomainLimits.UsernameMaxLength, MinimumLength = DomainLimits.UsernameMinLength)]
    [RegularExpression(DomainLimits.UsernamePattern, ErrorMessage = "Only letters, digits, dots, dashes and underscores are allowed.")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ChangePasswordForm
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(DomainLimits.PasswordMaxLength, MinimumLength = DomainLimits.PasswordMinLength)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm new password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class TotpForm
{
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [Display(Name = "Verification code")]
    public string? Code { get; set; }
}
