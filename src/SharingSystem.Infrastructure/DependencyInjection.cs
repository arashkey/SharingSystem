using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SharingSystem.Application.Abstractions;
using SharingSystem.Infrastructure.BackgroundJobs;
using SharingSystem.Infrastructure.Email;
using SharingSystem.Infrastructure.Persistence;
using SharingSystem.Infrastructure.Security;
using SharingSystem.Infrastructure.Storage;

namespace SharingSystem.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Default";
    private const string DatabaseFileName = "sharing.db";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string contentRootPath)
    {
        services.AddOptions<SecurityOptions>()
            .Bind(configuration.GetSection(SecurityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var storageOptions = configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions();
        var dataPathProvider = new DataPathProvider(contentRootPath, storageOptions);
        services.AddSingleton(dataPathProvider);

        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = $"Data Source={Path.Combine(dataPathProvider.DataPath, DatabaseFileName)}";
        }

        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ISecretProtector, AesGcmSecretProtector>();
        services.AddSingleton<ITotpService, TotpService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddHostedService<CleanupHostedService>();

        return services;
    }
}
