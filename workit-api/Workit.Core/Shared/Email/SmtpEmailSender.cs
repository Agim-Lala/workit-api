using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Workit.Core.Shared.EnvironmentUtils;

namespace Workit.Core.Shared.Email;

/// <summary>
/// Sends transactional email (e.g. registration confirmation links) over SMTP. Until
/// <c>SMTP_HOST</c> is configured, sends are skipped with a logged warning rather than attempting
/// the connection — the same "fail clearly, don't block the caller" shape used by
/// <see cref="Workit.Core.Shared.IdentityVerification.StripeIdentityVerificationProvider"/>, since
/// registration should still succeed even when outbound email isn't set up (e.g. local dev).
/// </summary>
public sealed class SmtpEmailSender(WorkitSettings settings, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!settings.Email.IsConfigured)
        {
            logger.LogWarning(
                "SMTP is not configured; skipping email {Subject} to {ToAddress}",
                subject,
                toAddress);
            return;
        }

        using var client = new SmtpClient(settings.Email.SmtpHost, settings.Email.SmtpPort)
        {
            EnableSsl = settings.Email.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(settings.Email.SmtpUsername))
        {
            client.Credentials = new NetworkCredential(settings.Email.SmtpUsername, settings.Email.SmtpPassword);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(settings.Email.FromAddress, settings.Email.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toAddress);

        await client.SendMailAsync(message, cancellationToken);
    }
}
