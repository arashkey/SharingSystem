using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using SharingSystem.Application.Abstractions;
using SharingSystem.Application.Common;
using SharingSystem.Application.Settings;

namespace SharingSystem.Infrastructure.Email;

/// <summary>
/// Sends e-mails through the SMTP server configured in the admin panel.
/// </summary>
public sealed class SmtpEmailSender(ISettingsService settingsService) : IEmailSender
{
    private const int ImplicitTlsPort = 465;

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var smtp = await settingsService.GetSmtpConfigurationAsync(cancellationToken);
        if (!smtp.Enabled || string.IsNullOrWhiteSpace(smtp.Host) || string.IsNullOrWhiteSpace(smtp.FromEmail))
        {
            throw AppException.Validation("SMTP is not configured.");
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.AppName, smtp.FromEmail));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        if (smtp.AllowUnauthorizedCertificates)
        {
            client.ServerCertificateValidationCallback = (_, _, _, _) => true;
        }

        var socketOptions = smtp.Port == ImplicitTlsPort ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrEmpty(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);
    }
}
