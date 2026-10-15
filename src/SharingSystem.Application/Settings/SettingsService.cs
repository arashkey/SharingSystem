using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Options;
using SharingSystem.Domain.Constants;
using SharingSystem.Domain.Entities;

namespace SharingSystem.Application.Settings;

public interface ISettingsService
{
    Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateAppSettingsRequest request, CancellationToken cancellationToken = default);

    Task<SmtpConfiguration> GetSmtpConfigurationAsync(CancellationToken cancellationToken = default);
}

public sealed class SettingsService(
    IAppDbContext dbContext,
    IMemoryCache cache,
    ISecretProtector secretProtector,
    IOptions<UploadOptions> uploadOptions,
    TimeProvider timeProvider) : ISettingsService
{
    private const string CacheKey = "app-settings";
    private const int MinChunkSizeBytes = (int)ByteSize.BytesPerMegabyte;
    private const long MinShareSizeBytes = ByteSize.BytesPerMegabyte;
    private const int MaxSessionDurationHours = 365 * 24;
    private const int MaxPort = 65535;

    public async Task<AppSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out AppSettingsDto? cached) && cached is not null)
        {
            return cached;
        }

        var entity = await GetOrCreateEntityAsync(cancellationToken);
        var settings = entity.Adapt<AppSettingsDto>();
        cache.Set(CacheKey, settings);
        return settings;
    }

    public async Task UpdateAsync(UpdateAppSettingsRequest request, CancellationToken cancellationToken = default)
    {
        Validate(request);

        var entity = await GetOrCreateEntityAsync(cancellationToken);
        request.Adapt(entity);
        entity.AppUrl = string.IsNullOrWhiteSpace(request.AppUrl) ? null : request.AppUrl.Trim().TrimEnd('/');

        if (request.ClearSmtpPassword)
        {
            entity.SmtpPasswordEncrypted = null;
        }
        else if (!string.IsNullOrEmpty(request.NewSmtpPassword))
        {
            entity.SmtpPasswordEncrypted = secretProtector.Protect(request.NewSmtpPassword);
        }

        entity.UpdatedAt = timeProvider.GetUtcNow().UtcDateTime;
        await dbContext.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);
    }

    public async Task<SmtpConfiguration> GetSmtpConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == AppSettingsEntity.SingletonId, cancellationToken)
            ?? new AppSettingsEntity();

        var password = entity.SmtpPasswordEncrypted is null
            ? null
            : secretProtector.Unprotect(entity.SmtpPasswordEncrypted);

        return new SmtpConfiguration(
            entity.SmtpEnabled,
            entity.SmtpHost,
            entity.SmtpPort,
            entity.SmtpFromEmail,
            entity.SmtpUsername,
            password,
            entity.SmtpAllowUnauthorizedCertificates,
            entity.AppName);
    }

    private async Task<AppSettingsEntity> GetOrCreateEntityAsync(CancellationToken cancellationToken)
    {
        var entity = await dbContext.AppSettings
            .FirstOrDefaultAsync(s => s.Id == AppSettingsEntity.SingletonId, cancellationToken);

        if (entity is not null)
        {
            return entity;
        }

        entity = new AppSettingsEntity { UpdatedAt = timeProvider.GetUtcNow().UtcDateTime };
        dbContext.AppSettings.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    private void Validate(UpdateAppSettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AppName) || request.AppName.Length > DomainLimits.AppNameMaxLength)
        {
            throw AppException.Validation($"The application name must contain 1 to {DomainLimits.AppNameMaxLength} characters.");
        }

        if (!string.IsNullOrWhiteSpace(request.AppUrl)
            && (!Uri.TryCreate(request.AppUrl.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            throw AppException.Validation("The application URL must be an absolute http(s) URL.");
        }

        if (request.SessionDurationHours is < 1 or > MaxSessionDurationHours)
        {
            throw AppException.Validation($"The session duration must be between 1 and {MaxSessionDurationHours} hours.");
        }

        if (request.MaxShareSizeBytes < MinShareSizeBytes)
        {
            throw AppException.Validation($"The maximum share size must be at least {ByteSize.Format(MinShareSizeBytes)}.");
        }

        if (request.MaxExpirationHours < 0)
        {
            throw AppException.Validation("The maximum expiration cannot be negative.");
        }

        var maxChunkSize = uploadOptions.Value.MaxRequestBodyBytes;
        if (request.ChunkSizeBytes < MinChunkSizeBytes || request.ChunkSizeBytes > maxChunkSize)
        {
            throw AppException.Validation(
                $"The chunk size must be between {ByteSize.Format(MinChunkSizeBytes)} and {ByteSize.Format(maxChunkSize)}.");
        }

        if (request.SmtpEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.SmtpHost) || string.IsNullOrWhiteSpace(request.SmtpFromEmail))
            {
                throw AppException.Validation("SMTP host and sender e-mail are required when SMTP is enabled.");
            }

            if (request.SmtpPort is < 1 or > MaxPort)
            {
                throw AppException.Validation("The SMTP port is invalid.");
            }
        }
    }
}
