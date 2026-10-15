namespace SharingSystem.Application.Auth;

/// <summary>The data needed to build an authentication session.</summary>
public sealed class AuthenticatedUser
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsAdmin { get; init; }
    public string SecurityStamp { get; init; } = string.Empty;
}

public sealed class RegisterRequest
{
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed record CredentialsResult(AuthenticatedUser User, bool RequiresTotp);
