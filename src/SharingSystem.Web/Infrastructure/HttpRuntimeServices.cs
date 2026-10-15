using System.Security.Claims;
using Microsoft.AspNetCore.StaticFiles;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Settings;

namespace SharingSystem.Web.Infrastructure;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public bool IsAdmin => Principal?.IsInRole(WebConstants.AdminRole) == true;
}

/// <summary>
/// Uses the configured application URL and falls back to the URL of the current request.
/// </summary>
public sealed class AppUrlResolver(IHttpContextAccessor httpContextAccessor, ISettingsService settingsService) : IAppUrlResolver
{
    public async Task<string> GetBaseUrlAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(settings.AppUrl))
        {
            return settings.AppUrl.TrimEnd('/');
        }

        var request = httpContextAccessor.HttpContext?.Request
                      ?? throw new InvalidOperationException("The application URL is not configured and there is no request.");
        return $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/');
    }
}

public sealed class ContentTypeResolver : IContentTypeResolver
{
    private const string DefaultContentType = "application/octet-stream";
    private readonly FileExtensionContentTypeProvider _provider = new();

    public string Resolve(string fileName) =>
        _provider.TryGetContentType(fileName, out var contentType) ? contentType : DefaultContentType;
}
