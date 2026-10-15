using Mapster;
using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Notifications;
using SharingSystem.Application.Settings;
using SharingSystem.Application.Users;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Auth;

public interface IAuthService
{
    /// <summary>Registration is possible when it is enabled or when no user exists yet (initial setup).</summary>
    Task<bool> CanRegisterAsync(CancellationToken cancellationToken = default);

    Task<bool> IsInitialSetupAsync(CancellationToken cancellationToken = default);

    Task<AuthenticatedUser> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

    Task<CredentialsResult> ValidateCredentialsAsync(string emailOrUsername, string password, CancellationToken cancellationToken = default);

    Task<AuthenticatedUser> VerifyTotpAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>Returns the user of an existing session or <c>null</c> when the session is no longer valid.</summary>
    Task<AuthenticatedUser?> GetSessionUserAsync(Guid userId, string securityStamp, CancellationToken cancellationToken = default);

    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}

public sealed class AuthService(
    IAppDbContext dbContext,
    IPasswordHasher passwordHasher,
    ISecretProtector secretProtector,
    ITotpService totpService,
    ISettingsService settingsService,
    INotificationService notificationService,
    IAppUrlResolver appUrlResolver,
    TimeProvider timeProvider) : IAuthService
{
    private const string InvalidCredentialsMessage = "Wrong e-mail/username or password.";
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    public async Task<bool> CanRegisterAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        return settings.AllowRegistration || await IsInitialSetupAsync(cancellationToken);
    }

    public async Task<bool> IsInitialSetupAsync(CancellationToken cancellationToken = default) =>
        !await dbContext.Users.AnyAsync(cancellationToken);

    public async Task<AuthenticatedUser> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!await CanRegisterAsync(cancellationToken))
        {
            throw AppException.Forbidden("Registration is disabled.");
        }

        var username = request.Username.Trim();
        UserValidation.ValidateUsername(username);
        UserValidation.ValidateEmail(request.Email);
        UserValidation.ValidatePassword(request.Password);

        var email = UserValidation.NormalizeEmail(request.Email);
        await UserValidation.EnsureUniqueAsync(dbContext, username, email, null, cancellationToken);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            // The very first user becomes the administrator.
            IsAdmin = await IsInitialSetupAsync(cancellationToken),
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Adapt<AuthenticatedUser>();
    }

    public async Task<CredentialsResult> ValidateCredentialsAsync(
        string emailOrUsername, string password, CancellationToken cancellationToken = default)
    {
        var login = emailOrUsername.Trim();
        var email = UserValidation.NormalizeEmail(login);

        var user = await dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email || u.Username == login, cancellationToken);

        if (user is null || !passwordHasher.Verify(user.PasswordHash, password))
        {
            throw AppException.Validation(InvalidCredentialsMessage);
        }

        return new CredentialsResult(user.Adapt<AuthenticatedUser>(), user.TotpEnabled);
    }

    public async Task<AuthenticatedUser> VerifyTotpAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null || !user.TotpEnabled || user.TotpSecretEncrypted is null)
        {
            throw AppException.Validation("The sign-in session expired. Please sign in again.");
        }

        var secret = secretProtector.Unprotect(user.TotpSecretEncrypted);
        if (!totpService.VerifyCode(secret, code))
        {
            throw AppException.Validation("The verification code is invalid.");
        }

        return user.Adapt<AuthenticatedUser>();
    }

    public async Task<AuthenticatedUser?> GetSessionUserAsync(
        Guid userId, string securityStamp, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == userId && u.SecurityStamp == securityStamp)
            .ProjectToType<AuthenticatedUser>()
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (!settings.CanSendEmails)
        {
            throw AppException.Validation("Password reset by e-mail is not available. Please contact an administrator.");
        }

        var normalizedEmail = UserValidation.NormalizeEmail(email);
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Do not reveal whether the e-mail address exists.
        if (user is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.PasswordResetTokens.Where(t => t.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);

        var token = SecureTokens.CreateUrlSafeToken();
        dbContext.PasswordResetTokens.Add(new PasswordResetTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = SecureTokens.Hash(token),
            CreatedAt = now,
            ExpiresAt = now.Add(PasswordResetTokenLifetime)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken);
        await notificationService.SendPasswordResetAsync(user.Email, $"{baseUrl}/auth/reset-password/{token}", cancellationToken);
    }

    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        UserValidation.ValidatePassword(newPassword);

        var tokenHash = SecureTokens.Hash(token);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var resetToken = await dbContext.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (resetToken?.User is null || resetToken.ExpiresAt <= now)
        {
            throw AppException.Validation("The password reset link is invalid or expired.");
        }

        resetToken.User.PasswordHash = passwordHasher.Hash(newPassword);
        resetToken.User.SecurityStamp = Guid.NewGuid().ToString("N");
        dbContext.PasswordResetTokens.Remove(resetToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
