using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Mapster;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using SharingSystem.Application;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Options;
using SharingSystem.Infrastructure;
using SharingSystem.Infrastructure.Persistence;
using SharingSystem.Infrastructure.Storage;
using SharingSystem.Web.Infrastructure;
using SharingSystem.Web.Localization;

const string DataProtectionKeysFolder = "keys";
const string ApplicationName = "SharingSystem";

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddApplication(configuration);
builder.Services.AddInfrastructure(configuration, builder.Environment.ContentRootPath);
TypeAdapterConfig.GlobalSettings.Scan(typeof(Program).Assembly);

// Large files are uploaded in chunks, so a single request never has to be bigger than one chunk.
var uploadOptions = configuration.GetSection(UploadOptions.SectionName).Get<UploadOptions>() ?? new UploadOptions();
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = uploadOptions.MaxRequestBodyBytes);
builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = uploadOptions.MaxRequestBodyBytes);

builder.Services.AddOptions<RateLimitingOptions>()
    .Bind(configuration.GetSection(RateLimitingOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<CookieLifetimeOptions>()
    .Bind(configuration.GetSection(CookieLifetimeOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAppLocalization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddScoped<IAppUrlResolver, AppUrlResolver>();
builder.Services.AddSingleton<IContentTypeResolver, ContentTypeResolver>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<ShareAccessService>();

// Data protection keys sign cookies; they are kept in the data folder so sessions survive restarts.
var dataPath = new DataPathProvider(
    builder.Environment.ContentRootPath,
    configuration.GetSection(StorageOptions.SectionName).Get<StorageOptions>() ?? new StorageOptions()).DataPath;
builder.Services.AddDataProtection()
    .SetApplicationName(ApplicationName)
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(dataPath, DataProtectionKeysFolder)));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = WebConstants.AuthCookieName;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/auth/sign-in";
        options.AccessDeniedPath = "/error/403";
        options.Events.OnValidatePrincipal = SessionValidator.ValidateAsync;
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(WebConstants.AdminPolicy, policy => policy.RequireRole(WebConstants.AdminRole));

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = WebConstants.AntiforgeryHeader;
    options.Cookie.Name = WebConstants.AntiforgeryCookieName;
});

builder.Services.AddRateLimiter(options =>
{
    var rateLimiting = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(WebConstants.AuthRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = rateLimiting.AuthPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimiting.AuthWindowSeconds)
            }));
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    // Trust the reverse proxy in front of the container (configure KnownProxies/KnownNetworks for stricter setups).
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services
    .AddControllersWithViews(options =>
    {
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        options.Filters.Add<AppExceptionFilter>();
    })
    .AddViewLocalization()
    .AddAppDataAnnotationsLocalization()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");

app.Use(async (context, next) =>
{
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers.XFrameOptions = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseRequestLocalization();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
