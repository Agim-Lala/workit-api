namespace Workit.Core.Workers.Domain;

/// <summary>
/// Tracks a worker's ID-verification badge: one row per worker profile, created the first time
/// they start verification and updated as the third-party provider's hosted flow resolves.
/// </summary>
public sealed class WorkerVerification
{
    public const int MaxProviderLength = 50;
    public const int MaxProviderReferenceIdLength = 200;
    public const int MaxRejectionReasonLength = 500;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid WorkerProfileId { get; private set; }
    public WorkerVerificationStatus Status { get; private set; } = WorkerVerificationStatus.NotStarted;
    public string Provider { get; private set; } = string.Empty;
    public string ProviderReferenceId { get; private set; } = string.Empty;
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private WorkerVerification()
    {
    }

    public WorkerVerification(Guid workerProfileId, DateTimeOffset createdAt)
    {
        WorkerProfileId = workerProfileId;
        CreatedAt = createdAt;
    }

    /// <summary>Records that a new hosted verification session was started with a provider.</summary>
    public void MarkPending(string provider, string providerReferenceId, DateTimeOffset now)
    {
        if (Status == WorkerVerificationStatus.Verified)
        {
            throw new InvalidOperationException("An already-verified worker cannot restart verification.");
        }

        Status = WorkerVerificationStatus.Pending;
        Provider = provider;
        ProviderReferenceId = providerReferenceId;
        SubmittedAt = now;
        DecidedAt = null;
        RejectionReason = null;
    }

    public void MarkVerified(DateTimeOffset now)
    {
        Status = WorkerVerificationStatus.Verified;
        DecidedAt = now;
        RejectionReason = null;
    }

    public void MarkRejected(string? reason, DateTimeOffset now)
    {
        Status = WorkerVerificationStatus.Rejected;
        DecidedAt = now;
        RejectionReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim()[..Math.Min(reason.Trim().Length, MaxRejectionReasonLength)];
    }
}
