using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Workit.Core.Shared.IdentityVerification;

/// <summary>
/// Verifies the <c>Stripe-Signature</c> header Stripe sends with each webhook delivery:
/// <c>t=&lt;unix-seconds&gt;,v1=&lt;hex-hmac-sha256&gt;</c> (comma-separated, possibly several
/// <c>v1=</c> entries when a secret was recently rotated), where the signed value is
/// <c>"{t}.{rawRequestBody}"</c> hashed with the webhook signing secret.
/// See https://docs.stripe.com/webhooks#verify-manually.
/// </summary>
public static class StripeWebhookSignature
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

    public static bool IsValid(string? signatureHeader, string rawBody, string webhookSecret, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader) || string.IsNullOrWhiteSpace(webhookSecret))
        {
            return false;
        }

        var parts = ParseHeader(signatureHeader);

        if (parts.Timestamp is null || parts.Signatures.Count == 0)
        {
            return false;
        }

        var signedAt = DateTimeOffset.FromUnixTimeSeconds(parts.Timestamp.Value);
        if ((now - signedAt).Duration() > MaxAge)
        {
            return false;
        }

        var expected = ComputeSignature(parts.Timestamp.Value, rawBody, webhookSecret);
        return parts.Signatures.Any(signature =>
            CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(signature),
                Convert.FromHexString(expected)));
    }

    private static string ComputeSignature(long timestamp, string rawBody, string webhookSecret)
    {
        var signedPayload = $"{timestamp}.{rawBody}";
        var key = Encoding.UTF8.GetBytes(webhookSecret);
        var hash = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signedPayload));
        return Convert.ToHexStringLower(hash);
    }

    private static (long? Timestamp, List<string> Signatures) ParseHeader(string header)
    {
        long? timestamp = null;
        var signatures = new List<string>();

        foreach (var segment in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = segment.Split('=', 2);
            if (pair.Length != 2)
            {
                continue;
            }

            switch (pair[0])
            {
                case "t" when long.TryParse(pair[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
                    timestamp = parsed;
                    break;
                case "v1":
                    signatures.Add(pair[1]);
                    break;
            }
        }

        return (timestamp, signatures);
    }
}
