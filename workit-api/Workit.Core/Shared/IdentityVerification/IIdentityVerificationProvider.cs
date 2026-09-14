namespace Workit.Core.Shared.IdentityVerification;

public sealed record VerificationSession(string ProviderReferenceId, string HostedUrl);

/// <summary>
/// Starts a hosted identity-verification flow with a third-party KYC vendor. The worker completes
/// the document/selfie capture entirely on the vendor's hosted page; Workit only creates the
/// session and later reacts to the vendor's outcome webhook.
/// </summary>
public interface IIdentityVerificationProvider
{
    /// <summary>The vendor name stored alongside the verification record, e.g. <c>"Persona"</c>.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Creates a new hosted verification session for <paramref name="referenceId"/> (the worker
    /// profile id, used to correlate the vendor's webhook back to the right worker).
    /// Throws <see cref="Workit.Core.Shared.Exceptions.DomainException"/> when the provider has
    /// no credentials configured or the vendor call fails.
    /// </summary>
    Task<VerificationSession> StartAsync(Guid referenceId, CancellationToken cancellationToken = default);
}
