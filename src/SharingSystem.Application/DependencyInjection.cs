using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SharingSystem.Application.Account;
using SharingSystem.Application.Admin;
using SharingSystem.Application.Auth;
using SharingSystem.Application.Maintenance;
using SharingSystem.Application.Notifications;
using SharingSystem.Application.Options;
using SharingSystem.Application.ReverseShares;
using SharingSystem.Application.Settings;
using SharingSystem.Application.Shares;
using SharingSystem.Application.Users;

namespace SharingSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);

        services.AddOptions<UploadOptions>()
            .Bind(configuration.GetSection(UploadOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CleanupOptions>()
            .Bind(configuration.GetSection(CleanupOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IShareRemover, ShareRemover>();
        services.AddScoped<IShareUploadService, ShareUploadService>();
        services.AddScoped<IShareViewerService, ShareViewerService>();
        services.AddScoped<IShareManagementService, ShareManagementService>();
        services.AddScoped<IReverseShareService, ReverseShareService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICleanupService, CleanupService>();

        return services;
    }
}
