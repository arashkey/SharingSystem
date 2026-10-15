namespace SharingSystem.Application.Abstractions;

/// <summary>Information about the user of the current request.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsAdmin { get; }
}

/// <summary>Resolves the public base URL of the application (without a trailing slash).</summary>
public interface IAppUrlResolver
{
    Task<string> GetBaseUrlAsync(CancellationToken cancellationToken = default);
}

/// <summary>Resolves the MIME type of a file from its name.</summary>
public interface IContentTypeResolver
{
    string Resolve(string fileName);
}

/// <summary>Sends plain text e-mails using the configured SMTP server.</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
