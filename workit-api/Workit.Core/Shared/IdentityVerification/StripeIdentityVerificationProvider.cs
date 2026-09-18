using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Resiliency;

namespace Workit.Core.Shared.IdentityVerification;

/// <summary>
/// Starts a Stripe Identity hosted "VerificationSession" (https://docs.stripe.com/identity).
/// Test-mode secret keys are free to create and work immediately — no sales call, no minimum
/// spend; live mode is pay-per-verification. Requires a Stripe account with Identity activated;
/// until <c>STRIPE_SECRET_KEY</c> is configured, verification requests fail with a clear domain
/// error instead of attempting the call.
/// </summary>
public sealed class StripeIdentityVerificationProvider(
    HttpClient httpClient,
    WorkitSettings settings,
    IResilienceHandler resilienceHandler) : IIdentityVerificationProvider
{
    public string ProviderName => "Stripe";

    public async Task<VerificationSession> StartAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        if (!settings.StripeIdentity.IsConfigured)
        {
            throw new DomainException("error.verificationNotConfigured");
        }

        // Stripe's REST API takes form-encoded bodies, with bracket notation for nested fields.
        var request = new HttpRequestMessage(HttpMethod.Post, "identity/verification_sessions")
        {
            Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("type", "document"),
                new KeyValuePair<string, string>("metadata[worker_profile_id]", referenceId.ToString())
            ])
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.StripeIdentity.SecretKey);

        try
        {
            var response = await resilienceHandler.HandleWithRetryAsync(
                async token =>
                {
                    var result = await httpClient.SendAsync(request, token);
                    result.EnsureSuccessStatusCode();
                    return result;
                },
                cancellationToken);

            var payload = await response.Content.ReadFromJsonAsync<VerificationSessionResponse>(cancellationToken);

            if (string.IsNullOrWhiteSpace(payload?.Id) || string.IsNullOrWhiteSpace(payload.Url))
            {
                throw new DomainException("error.verificationStartFailed");
            }

            return new VerificationSession(payload.Id, payload.Url);
        }
        catch (HttpRequestException)
        {
            throw new DomainException("error.verificationStartFailed");
        }
    }

    private sealed class VerificationSessionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("url")]
        public string? Url { get; init; }
    }
}
