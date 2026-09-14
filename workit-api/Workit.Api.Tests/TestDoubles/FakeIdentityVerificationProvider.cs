using Workit.Core.Shared.IdentityVerification;

namespace Workit.Api.Tests.TestDoubles;

/// <summary>
/// Test double for <see cref="IIdentityVerificationProvider"/> so API tests never call the real
/// Persona API (which also has no sandbox credentials configured in this environment anyway).
/// </summary>
public sealed class FakeIdentityVerificationProvider : IIdentityVerificationProvider
{
    public string ProviderName => "FakeProvider";

    public Task<VerificationSession> StartAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new VerificationSession($"fake-inquiry-{referenceId}", $"https://verify.test/{referenceId}"));
    }
}
