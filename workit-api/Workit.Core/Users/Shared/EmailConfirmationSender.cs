using Microsoft.Extensions.Logging;
using Workit.Core.Shared.Email;
using Workit.Core.Shared.EnvironmentUtils;

namespace Workit.Core.Users.Shared;

/// <summary>
/// Builds and sends the "confirm your email" link shared by worker and business registration,
/// via the Resend template with alias <c>confirm-email</c>
/// (see docs/email-templates/confirm-email.html for its source). Delivery failures are logged,
/// never thrown — a broken/unconfigured mail provider must not block account creation.
/// </summary>
public static class EmailConfirmationSender
{
    private const string TemplateId = "confirm-email";

    public static async Task SendAsync(
        IEmailSender emailSender,
        WorkitSettings settings,
        ILogger logger,
        string toEmail,
        string plainToken,
        CancellationToken cancellationToken)
    {
        var confirmationLink = $"{settings.Email.ConfirmationLinkBaseUrl}?token={Uri.EscapeDataString(plainToken)}";
        var variables = new Dictionary<string, string>
        {
            ["confirmationLink"] = confirmationLink,
            ["expirationInHours"] = settings.Email.ConfirmationTokenExpirationInHours.ToString()
        };

        try
        {
            await emailSender.SendTemplateAsync(toEmail, TemplateId, variables, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Failed to send confirmation email to {Email}", toEmail);
        }
    }
}
