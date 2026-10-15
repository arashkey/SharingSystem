using System.ComponentModel.DataAnnotations;
using SharingSystem.Domain.Constants;

namespace SharingSystem.Web.Models;

public sealed class SignInViewModel
{
    [Required]
    [Display(Name = "E-mail or username")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }

    public bool CanRegister { get; set; }

    public bool CanResetPassword { get; set; }
}

public sealed class SignUpViewModel
{
    [Required]
    [StringLength(DomainLimits.UsernameMaxLength, MinimumLength = DomainLimits.UsernameMinLength)]
    [RegularExpression(DomainLimits.UsernamePattern, ErrorMessage = "Only letters, digits, dots, dashes and underscores are allowed.")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(DomainLimits.EmailMaxLength)]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(DomainLimits.PasswordMaxLength, MinimumLength = DomainLimits.PasswordMinLength)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public bool IsInitialSetup { get; set; }
}

public sealed class TotpViewModel
{
    [Required]
    [StringLength(DomainLimits.TotpCodeLength, MinimumLength = DomainLimits.TotpCodeLength)]
    [Display(Name = "Verification code")]
    public string Code { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public sealed class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(DomainLimits.PasswordMaxLength, MinimumLength = DomainLimits.PasswordMinLength)]
    [Display(Name = "New password")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
