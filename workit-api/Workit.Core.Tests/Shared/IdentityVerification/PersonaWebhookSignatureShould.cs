using System.Security.Cryptography;
using System.Text;
using Shouldly;
using Workit.Core.Shared.IdentityVerification;

namespace Workit.Core.Tests.Shared.IdentityVerification;

public sealed class PersonaWebhookSignatureShould
{
    private const string Secret = "test-webhook-secret";

    [Fact]
    public void AcceptAFreshlySignedPayload()
    {
        var now = DateTimeOffset.UtcNow;
        var body = """{"data":{"attributes":{"name":"inquiry.approved"}}}""";
        var header = SignHeader(body, now.ToUnixTimeSeconds());

        PersonaWebhookSignature.IsValid(header, body, Secret, now).ShouldBeTrue();
    }

    [Fact]
    public void RejectAnUnsignedRequest()
    {
        PersonaWebhookSignature.IsValid(null, "{}", Secret, DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void RejectATamperedBody()
    {
        var now = DateTimeOffset.UtcNow;
        var header = SignHeader("""{"original":true}""", now.ToUnixTimeSeconds());

        PersonaWebhookSignature.IsValid(header, """{"tampered":true}""", Secret, now).ShouldBeFalse();
    }

    [Fact]
    public void RejectAnExpiredSignature()
    {
        var signedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var body = "{}";
        var header = SignHeader(body, signedAt.ToUnixTimeSeconds());

        PersonaWebhookSignature.IsValid(header, body, Secret, DateTimeOffset.UtcNow).ShouldBeFalse();
    }

    [Fact]
    public void RejectAWrongSecret()
    {
        var now = DateTimeOffset.UtcNow;
        var body = "{}";
        var header = SignHeader(body, now.ToUnixTimeSeconds());

        PersonaWebhookSignature.IsValid(header, body, "a-different-secret", now).ShouldBeFalse();
    }

    private static string SignHeader(string body, long timestamp)
    {
        var signedPayload = $"{timestamp}.{body}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(Secret), Encoding.UTF8.GetBytes(signedPayload));
        return $"t={timestamp},v1={Convert.ToHexStringLower(hash)}";
    }
}
