using Mapster;
using Microsoft.EntityFrameworkCore;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Notifications;
using SharingSystem.Application.Shares;
using SharingSystem.Domain.Constants;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Users;

public sealed class UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public bool TotpEnabled { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ShareCount { get; init; }
}

public sealed class CreateUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;

    /// <summary>Initial password. When empty, a random password is generated and sent by e-mail.</summary>
    public string? Password { get; init; }

    public bool IsAdmin { get; init; }
}

public sealed class UpdateUserRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public string? NewPassword { get; init; }
    public bool DisableTotp { get; init; }
}

public interface IUserAdminService
{
    Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// User management for administrators.
/// </summary>
public sealed class UserAdminService(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher,
    INotificationService notificationService,
    IShareRemover shareRemover,
    TimeProvider timeProvider) : IUserAdminService
{
    private const int GeneratedPasswordLength = 16;

    public async Task<IReadOnlyList<UserDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        return await dbContext.Users.AsNoTracking()
            .OrderBy(u => u.Username)
            .ProjectToType<UserDto>()
            .ToListAsync(cancellationToken);
    }

    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        return await dbContext.Users.AsNoTracking()
                   .Where(u => u.Id == id)
                   .ProjectToType<UserDto>()
                   .FirstOrDefaultAsync(cancellationToken)
               ?? throw AppException.NotFound("The user does not exist.");
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var username = request.Username.Trim();
        UserValidation.ValidateUsername(username);
        UserValidation.ValidateEmail(request.Email);

        var sendInvite = string.IsNullOrEmpty(request.Password);
        var password = sendInvite ? SecureTokens.CreateAlphaNumeric(GeneratedPasswordLength) : request.Password!;
        UserValidation.ValidatePassword(password);

        var email = UserValidation.NormalizeEmail(request.Email);
        await UserValidation.EnsureUniqueAsync(dbContext, username, email, null, cancellationToken);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            IsAdmin = request.IsAdmin,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        if (sendInvite)
        {
            // Send first, so that no account without a known password is created when e-mail fails.
            await notificationService.SendInviteAsync(email, password, cancellationToken);
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user.Adapt<UserDto>();
    }

    public async Task UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw AppException.NotFound("The user does not exist.");

        var username = request.Username.Trim();
        UserValidation.ValidateUsername(username);
        UserValidation.ValidateEmail(request.Email);
        var email = UserValidation.NormalizeEmail(request.Email);
        await UserValidation.EnsureUniqueAsync(dbContext, username, email, user.Id, cancellationToken);

        if (user.IsAdmin && !request.IsAdmin)
        {
            await EnsureAnotherAdminExistsAsync(user.Id, cancellationToken);
        }

        var securityChanged = user.IsAdmin != request.IsAdmin;
        user.Username = username;
        user.Email = email;
        user.IsAdmin = request.IsAdmin;

        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            UserValidation.ValidatePassword(request.NewPassword);
            user.PasswordHash = passwordHasher.Hash(request.NewPassword);
            securityChanged = true;
        }

        if (request.DisableTotp && user.TotpEnabled)
        {
            user.TotpEnabled = false;
            user.TotpSecretEncrypted = null;
            securityChanged = true;
        }

        if (securityChanged)
        {
            user.SecurityStamp = Guid.NewGuid().ToString("N");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAdmin();
        if (currentUser.UserId == id)
        {
            throw AppException.Validation("You cannot delete your own account.");
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw AppException.NotFound("The user does not exist.");

        if (user.IsAdmin)
        {
            await EnsureAnotherAdminExistsAsync(user.Id, cancellationToken);
        }

        var shares = await dbContext.Shares.Where(s => s.CreatorId == user.Id).ToListAsync(cancellationToken);
        await shareRemover.RemoveAsync(shares, cancellationToken);
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAnotherAdminExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.AnyAsync(u => u.IsAdmin && u.Id != userId, cancellationToken))
        {
            throw AppException.Validation("At least one administrator must remain.");
        }
    }

    private void EnsureAdmin()
    {
        if (!currentUser.IsAdmin)
        {
            throw AppException.Forbidden();
        }
    }
}
