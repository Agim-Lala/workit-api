using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.Resiliency;

namespace Workit.Core.Shared.Email;

/// <summary>
/// Sends transactional email via a published Resend template
/// (https://resend.com/docs/api-reference/emails/send-email#template). Until
/// <c>RESEND_API_KEY</c> is configured, sends are skipped with a logged warning rather than
/// attempting the call — the same "fail clearly, don't block the caller" shape used by
/// <see cref="Workit.Core.Shared.IdentityVerification.StripeIdentityVerificationProvider"/>, since
/// registration should still succeed even when outbound email isn't set up (e.g. local dev).
/// Subject is whatever the template is configured with in the Resend dashboard — not sent here.
/// </summary>
public sealed class ResendEmailSender(
    HttpClient httpClient,
    WorkitSettings settings,
    IResilienceHandler resilienceHandler,
    ILogger<ResendEmailSender> logger) : IEmailSender
{
    public async Task SendTemplateAsync(
        string toAddress,
        string templateId,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        if (!settings.Email.IsConfigured)
        {
            logger.LogWarning(
                "Resend is not configured; skipping template {TemplateId} email to {ToAddress}",
                templateId,
                toAddress);
            return;
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "emails")
        {
            Content = JsonContent.Create(new
            {
                from = $"{settings.Email.FromName} <{settings.Email.FromAddress}>",
                to = new[] { toAddress },
                template = new { id = templateId, variables }
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.Email.ApiKey);

        await resilienceHandler.HandleWithRetryAsync(
            async token =>
            {
                var response = await httpClient.SendAsync(request, token);
                response.EnsureSuccessStatusCode();
                return response;
            },
            cancellationToken);
    }
}
