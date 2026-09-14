using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Resiliency;

namespace Workit.Core.Shared.IdentityVerification;

/// <summary>
/// Starts a Persona hosted "inquiry" (https://docs.withpersona.com) for the worker
/// identity-verification badge. Requires a Persona account with an inquiry template; until
/// <c>PERSONA_API_KEY</c> and <c>PERSONA_INQUIRY_TEMPLATE_ID</c> are configured, verification
/// requests fail with a clear domain error instead of attempting the call.
/// </summary>
public sealed class PersonaIdentityVerificationProvider(
    HttpClient httpClient,
    WorkitSettings settings,
    IResilienceHandler resilienceHandler) : IIdentityVerificationProvider
{
    public string ProviderName => "Persona";

    public async Task<VerificationSession> StartAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        if (!settings.Persona.IsConfigured)
        {
            throw new DomainException("error.verificationNotConfigured");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, "inquiries")
        {
            Content = JsonContent.Create(new CreateInquiryRequest(
                new CreateInquiryRequest.InquiryData(
                    new CreateInquiryRequest.InquiryAttributes(
                        settings.Persona.InquiryTemplateId!,
                        referenceId.ToString())),
                new CreateInquiryRequest.InquiryMeta(true, true)))
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.Persona.ApiKey);

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

            var payload = await response.Content.ReadFromJsonAsync<CreateInquiryResponse>(cancellationToken);
            var inquiryId = payload?.Data?.Id;
            var hostedUrl = payload?.Meta?.OneTimeLink;

            if (string.IsNullOrWhiteSpace(inquiryId) || string.IsNullOrWhiteSpace(hostedUrl))
            {
                throw new DomainException("error.verificationStartFailed");
            }

            return new VerificationSession(inquiryId, hostedUrl);
        }
        catch (HttpRequestException)
        {
            throw new DomainException("error.verificationStartFailed");
        }
    }

    private sealed record CreateInquiryRequest(
        [property: JsonPropertyName("data")] CreateInquiryRequest.InquiryData Data,
        [property: JsonPropertyName("meta")] CreateInquiryRequest.InquiryMeta Meta)
    {
        public sealed record InquiryData(
            [property: JsonPropertyName("attributes")] InquiryAttributes Attributes);

        public sealed record InquiryAttributes(
            [property: JsonPropertyName("inquiry-template-id")] string InquiryTemplateId,
            [property: JsonPropertyName("reference-id")] string ReferenceId);

        public sealed record InquiryMeta(
            [property: JsonPropertyName("auto-create-inquiry-session")] bool AutoCreateInquirySession,
            [property: JsonPropertyName("auto-create-one-time-link")] bool AutoCreateOneTimeLink);
    }

    private sealed class CreateInquiryResponse
    {
        [JsonPropertyName("data")]
        public ResponseData? Data { get; init; }

        [JsonPropertyName("meta")]
        public ResponseMeta? Meta { get; init; }

        public sealed class ResponseData
        {
            [JsonPropertyName("id")]
            public string? Id { get; init; }
        }

        public sealed class ResponseMeta
        {
            [JsonPropertyName("one-time-link")]
            public string? OneTimeLink { get; init; }
        }
    }
}
