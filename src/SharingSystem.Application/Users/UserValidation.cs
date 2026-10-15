using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Domain.Constants;

namespace SharingSystem.Application.Users;

/// <summary>
/// Validation rules for user accounts shared by registration, profile and admin services.
/// </summary>
public static partial class UserValidation
{
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static void ValidateUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username)
            || username.Length < DomainLimits.UsernameMinLength
            || username.Length > DomainLimits.UsernameMaxLength
            || !UsernameRegex().IsMatch(username))
        {
            throw AppException.Validation(
                $"The username must contain {DomainLimits.UsernameMinLength} to {DomainLimits.UsernameMaxLength} " +
                "letters, digits, dots, dashes or underscores.");
        }
    }

    public static void ValidateEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)
            || email.Length > DomainLimits.EmailMaxLength
            || !MailAddress.TryCreate(email.Trim(), out _))
        {
            throw AppException.Validation("The e-mail address is invalid.");
        }
    }

    public static void ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password)
            || password.Length < DomainLimits.PasswordMinLength
            || password.Length > DomainLimits.PasswordMaxLength)
        {
            throw AppException.Validation(
                $"The password must contain {DomainLimits.PasswordMinLength} to {DomainLimits.PasswordMaxLength} characters.");
        }
    }

    public static async Task EnsureUniqueAsync(
        IAppDbContext dbContext, string username, string normalizedEmail, Guid? exceptUserId, CancellationToken cancellationToken)
    {
        var usernameTaken = await dbContext.Users.AnyAsync(
            u => u.Username == username && u.Id != exceptUserId, cancellationToken);
        if (usernameTaken)
        {
            throw AppException.Validation("This username is already taken.");
        }

        var emailTaken = await dbContext.Users.AnyAsync(
            u => u.Email == normalizedEmail && u.Id != exceptUserId, cancellationToken);
        if (emailTaken)
        {
            throw AppException.Validation("This e-mail address is already in use.");
        }
    }

    [GeneratedRegex(DomainLimits.UsernamePattern)]
    private static partial Regex UsernameRegex();
}
