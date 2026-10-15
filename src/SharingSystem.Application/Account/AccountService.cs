using Mapster;
using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Auth;
using SharingSystem.Application.Common;
using SharingSystem.Application.Settings;
using SharingSystem.Application.Users;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Account;

public sealed class AccountDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool TotpEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class UpdateProfileRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = string.Empty;
    public string NewPassword { get; init; } = string.Empty;
}

public sealed record TotpSetupDto(string Secret, string QrCodeDataUri);

public interface IAccountService
{
    Task<AccountDto> GetAsync(CancellationToken cancellationToken = default);

    Task<AuthenticatedUser> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default);

    Task<AuthenticatedUser> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task<TotpSetupDto> StartTotpSetupAsync(string password, CancellationToken cancellationToken = default);

    Task<AuthenticatedUser> EnableTotpAsync(string code, CancellationToken cancellationToken = default);

    Task<AuthenticatedUser> DisableTotpAsync(string password, string code, CancellationToken cancellationToken = default);
}

/// <summary>
/// Self-service operations of the signed-in user.
/// Methods that change security relevant data return the refreshed session user.
/// </summary>
public sealed class AccountService(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    ISecretProtector secretProtector,
    ITotpService totpService,
    ISettingsService settingsService) : IAccountService
{
    public async Task<AccountDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await LoadCurrentUserAsync(cancellationToken);
        return user.Adapt<AccountDto>();
    }

    public async Task<AuthenticatedUser> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadCurrentUserAsync(cancellationToken);
        var username = request.Username.Trim();
        UserValidation.ValidateUsername(username);
        UserValidation.ValidateEmail(request.Email);

        var email = UserValidation.NormalizeEmail(request.Email);
        await UserValidation.EnsureUniqueAsync(dbContext, username, email, user.Id, cancellationToken);

        user.Username = username;
        user.Email = email;
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Adapt<AuthenticatedUser>();
    }

    public async Task<AuthenticatedUser> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await LoadCurrentUserAsync(cancellationToken);
        EnsurePassword(user, request.CurrentPassword);
        UserValidation.ValidatePassword(request.NewPassword);

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Adapt<AuthenticatedUser>();
    }

    public async Task<TotpSetupDto> StartTotpSetupAsync(string password, CancellationToken cancellationToken = default)
    {
        var user = await LoadCurrentUserAsync(cancellationToken);
        EnsurePassword(user, password);

        if (user.TotpEnabled)
        {
            throw AppException.Validation("Two-factor authentication is already enabled.");
        }

        var secret = totpService.GenerateSecret();
        user.TotpSecretEncrypted = secretProtector.Protect(secret);
        await dbContext.SaveChangesAsync(cancellationToken);

        var settings = await settingsService.GetAsync(cancellationToken);
        var uri = totpService.BuildProvisioningUri(settings.AppName, user.Email, secret);
        return new TotpSetupDto(secret, totpService.GenerateQrCodeDataUri(uri));
    }

    public async Task<AuthenticatedUser> EnableTotpAsync(string code, CancellationToken cancellationToken = default)
    {
        var user = await LoadCurrentUserAsync(cancellationToken);
        if (user.TotpEnabled)
        {
            throw AppException.Validation("Two-factor authentication is already enabled.");
        }

        if (user.TotpSecretEncrypted is null)
        {
            throw AppException.Validation("Start the two-factor setup first.");
        }

        if (!totpService.VerifyCode(secretProtector.Unprotect(user.TotpSecretEncrypted), code))
        {
            throw AppException.Validation("The verification code is invalid.");
        }

        user.TotpEnabled = true;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Adapt<AuthenticatedUser>();
    }

    public async Task<AuthenticatedUser> DisableTotpAsync(string password, string code, CancellationToken cancellationToken = default)
    {
        var user = await LoadCurrentUserAsync(cancellationToken);
        EnsurePassword(user, password);

        if (!user.TotpEnabled || user.TotpSecretEncrypted is null)
        {
            throw AppException.Validation("Two-factor authentication is not enabled.");
        }

        if (!totpService.VerifyCode(secretProtector.Unprotect(user.TotpSecretEncrypted), code))
        {
            throw AppException.Validation("The verification code is invalid.");
        }

        user.TotpEnabled = false;
        user.TotpSecretEncrypted = null;
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Adapt<AuthenticatedUser>();
    }

    private async Task<UserEntity> LoadCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
               ?? throw AppException.NotFound("The user does not exist.");
    }

    private void EnsurePassword(UserEntity user, string password)
    {
        if (!passwordHasher.Verify(user.PasswordHash, password))
        {
            throw AppException.Validation("The current password is wrong.");
        }
    }
}
