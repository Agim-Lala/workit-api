using System.Text.Json;

namespace Workit.Core.Shared.IdentityVerification;

public sealed record StripeWebhookEvent(string EventType, string VerificationSessionId);

/// <summary>
/// Pulls the event type and VerificationSession id out of a Stripe webhook delivery:
/// <c>{ "type": "identity.verification_session.verified", "data": { "object": { "id": "vs_..." } } }</c>.
/// The outcome is read from the event <c>type</c> itself (Stripe fires a distinct event per
/// outcome), not from a separate status field.
/// </summary>
public static class StripeWebhookPayloadParser
{
    public static StripeWebhookEvent? TryParse(string rawBody)
    {
        try
        {
            using var document = JsonDocument.Parse(rawBody);
            var root = document.RootElement;

            if (!root.TryGetProperty("type", out var typeElement) || typeElement.GetString() is not { Length: > 0 } eventType)
            {
                return null;
            }

            if (!root.TryGetProperty("data", out var data)
                || !data.TryGetProperty("object", out var sessionObject)
                || !sessionObject.TryGetProperty("id", out var idElement)
                || idElement.GetString() is not { Length: > 0 } sessionId)
            {
                return null;
            }

            return new StripeWebhookEvent(eventType, sessionId);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
