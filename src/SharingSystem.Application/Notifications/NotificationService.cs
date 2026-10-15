using System.Globalization;
using Microsoft.Extensions.Logging;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Settings;

namespace SharingSystem.Application.Notifications;

public interface INotificationService
{
    Task SendShareToRecipientsAsync(
        IReadOnlyCollection<string> recipients, string shareUrl, string creator, string? description,
        DateTime? expiresAt, CancellationToken cancellationToken = default);

    Task SendReverseShareUsedAsync(string creatorEmail, string shareUrl, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default);

    Task SendInviteAsync(string email, string password, CancellationToken cancellationToken = default);

    Task SendTestEmailAsync(string email, CancellationToken cancellationToken = default);
}

/// <summary>
/// Renders e-mail templates configured by the administrator and sends them.
/// Failures of notification e-mails are logged and never break the main operation.
/// </summary>
public sealed class NotificationService(
    ISettingsService settingsService,
    IEmailSender emailSender,
    IAppUrlResolver appUrlResolver,
    ILogger<NotificationService> logger) : INotificationService
{
    private const string NeverExpires = "never";
    private const string NoDescription = "No description";
    private const string DateFormat = "yyyy-MM-dd HH:mm 'UTC'";

    public async Task SendShareToRecipientsAsync(
        IReadOnlyCollection<string> recipients, string shareUrl, string creator, string? description,
        DateTime? expiresAt, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (!settings.CanSendEmails || !settings.EnableShareEmailRecipients || recipients.Count == 0)
        {
            return;
        }

        var expires = expiresAt.HasValue
            ? "on " + expiresAt.Value.ToString(DateFormat, CultureInfo.InvariantCulture)
            : NeverExpires;

        var values = new Dictionary<string, string>
        {
            ["creator"] = creator,
            ["shareUrl"] = shareUrl,
            ["desc"] = string.IsNullOrWhiteSpace(description) ? NoDescription : description,
            ["expires"] = expires,
            ["appName"] = settings.AppName
        };

        foreach (var recipient in recipients)
        {
            await TrySendAsync(recipient, settings.ShareRecipientsSubject, Render(settings.ShareRecipientsMessage, values), cancellationToken);
        }
    }

    public async Task SendReverseShareUsedAsync(string creatorEmail, string shareUrl, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (!settings.CanSendEmails)
        {
            return;
        }

        var values = new Dictionary<string, string> { ["shareUrl"] = shareUrl, ["appName"] = settings.AppName };
        await TrySendAsync(creatorEmail, settings.ReverseShareSubject, Render(settings.ReverseShareMessage, values), cancellationToken);
    }

    public async Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        var values = new Dictionary<string, string> { ["url"] = resetUrl, ["appName"] = settings.AppName };
        await TrySendAsync(email, settings.ResetPasswordSubject, Render(settings.ResetPasswordMessage, values), cancellationToken);
    }

    public async Task SendInviteAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (!settings.CanSendEmails)
        {
            throw AppException.Validation("SMTP is not configured, so the invitation cannot be sent.");
        }

        var baseUrl = await appUrlResolver.GetBaseUrlAsync(cancellationToken);
        var values = new Dictionary<string, string>
        {
            ["url"] = $"{baseUrl}/auth/sign-in",
            ["password"] = password,
            ["email"] = email,
            ["appName"] = settings.AppName
        };

        await emailSender.SendAsync(email, settings.InviteSubject, Render(settings.InviteMessage, values), cancellationToken);
    }

    public async Task SendTestEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var settings = await settingsService.GetAsync(cancellationToken);
        if (!settings.CanSendEmails)
        {
            throw AppException.Validation("Enable and save the SMTP settings before sending a test e-mail.");
        }

        try
        {
            await emailSender.SendAsync(
                email,
                $"{settings.AppName}: test e-mail",
                "This is a test e-mail. Your SMTP configuration works.",
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Sending the test e-mail failed");
            throw AppException.Validation($"Sending the test e-mail failed: {exception.Message}");
        }
    }

    private async Task TrySendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            await emailSender.SendAsync(to, subject, body, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Sending an e-mail with subject {Subject} failed", subject);
        }
    }

    private static string Render(string template, IReadOnlyDictionary<string, string> values)
    {
        var result = template;
        foreach (var (key, value) in values)
        {
            result = result.Replace("{" + key + "}", value, StringComparison.Ordinal);
        }

        return result;
    }
}
