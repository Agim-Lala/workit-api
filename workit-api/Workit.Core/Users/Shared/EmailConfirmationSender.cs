using Microsoft.Extensions.Logging;
using Workit.Core.Shared.Email;
using Workit.Core.Shared.EnvironmentUtils;

namespace Workit.Core.Users.Shared;

/// <summary>
/// Builds and sends the "confirm your email" link shared by worker and business registration.
/// Delivery failures are logged, never thrown — a broken/unconfigured mail provider must not
/// block account creation.
/// </summary>
public static class EmailConfirmationSender
{
    public static async Task SendAsync(
        IEmailSender emailSender,
        WorkitSettings settings,
        ILogger logger,
        string toEmail,
        string plainToken,
        CancellationToken cancellationToken)
    {
        var confirmationLink = $"{settings.Email.ConfirmationLinkBaseUrl}?token={Uri.EscapeDataString(plainToken)}";
        const string subject = "Confirm your Workit email";
        var body = $"""
            <p>Welcome to Workit.</p>
            <p>Confirm your email address to finish setting up your account:</p>
            <p><a href="{confirmationLink}">{confirmationLink}</a></p>
            <p>This link expires in {settings.Email.ConfirmationTokenExpirationInHours} hours.</p>
            """;

        try
        {
            await emailSender.SendAsync(toEmail, subject, body, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to send confirmation email to {Email}", toEmail);
        }
    }
}
