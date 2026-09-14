using System.Text.Json;

namespace Workit.Core.Shared.IdentityVerification;

public sealed record PersonaWebhookEvent(string InquiryId, string Status);

/// <summary>
/// Pulls the inquiry id and status out of a Persona webhook delivery. Persona wraps the actual
/// inquiry resource inside an event envelope:
/// <c>data.attributes.payload.data.id</c> / <c>data.attributes.payload.data.attributes.status</c>.
/// Falls back to a flatter <c>data.id</c> / <c>data.attributes.status</c> shape defensively, in
/// case the envelope differs from what this integration was written against — re-check a real
/// sandbox delivery once Persona credentials are configured.
/// </summary>
public static class PersonaWebhookPayloadParser
{
    public static PersonaWebhookEvent? TryParse(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;

            var eventData = root.TryGetProperty("data", out var data) ? data : default;
            var payload = eventData.TryGetProperty("attributes", out var eventAttributes)
                && eventAttributes.TryGetProperty("payload", out var payloadElement)
                ? payloadElement
                : eventData;

            if (!payload.TryGetProperty("data", out var inquiry)
                || !inquiry.TryGetProperty("id", out var idElement)
                || idElement.GetString() is not { Length: > 0 } inquiryId)
            {
                return null;
            }

            var status = inquiry.TryGetProperty("attributes", out var inquiryAttributes)
                && inquiryAttributes.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : null;

            return string.IsNullOrWhiteSpace(status) ? null : new PersonaWebhookEvent(inquiryId, status);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
