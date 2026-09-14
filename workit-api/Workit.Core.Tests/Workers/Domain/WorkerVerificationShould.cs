using Shouldly;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Tests.Workers.Domain;

public sealed class WorkerVerificationShould
{
    [Fact]
    public void StartAsNotStarted()
    {
        var verification = new WorkerVerification(Guid.NewGuid(), DateTimeOffset.UtcNow);

        verification.Status.ShouldBe(WorkerVerificationStatus.NotStarted);
    }

    [Fact]
    public void RecordProviderSessionWhenMarkedPending()
    {
        var verification = new WorkerVerification(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var now = DateTimeOffset.UtcNow;

        verification.MarkPending("Persona", "inq_123", now);

        verification.Status.ShouldBe(WorkerVerificationStatus.Pending);
        verification.Provider.ShouldBe("Persona");
        verification.ProviderReferenceId.ShouldBe("inq_123");
        verification.SubmittedAt.ShouldBe(now);
        verification.DecidedAt.ShouldBeNull();
    }

    [Fact]
    public void ThrowWhenRestartingAnAlreadyVerifiedInquiry()
    {
        var verification = new WorkerVerification(Guid.NewGuid(), DateTimeOffset.UtcNow);
        verification.MarkPending("Persona", "inq_123", DateTimeOffset.UtcNow);
        verification.MarkVerified(DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() =>
            verification.MarkPending("Persona", "inq_456", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RecordDecisionWhenVerified()
    {
        var verification = new WorkerVerification(Guid.NewGuid(), DateTimeOffset.UtcNow);
        verification.MarkPending("Persona", "inq_123", DateTimeOffset.UtcNow);
        var decidedAt = DateTimeOffset.UtcNow;

        verification.MarkVerified(decidedAt);

        verification.Status.ShouldBe(WorkerVerificationStatus.Verified);
        verification.DecidedAt.ShouldBe(decidedAt);
        verification.RejectionReason.ShouldBeNull();
    }

    [Fact]
    public void RecordTruncatedReasonWhenRejected()
    {
        var verification = new WorkerVerification(Guid.NewGuid(), DateTimeOffset.UtcNow);
        verification.MarkPending("Persona", "inq_123", DateTimeOffset.UtcNow);
        var decidedAt = DateTimeOffset.UtcNow;
        var longReason = new string('x', WorkerVerification.MaxRejectionReasonLength + 50);

        verification.MarkRejected(longReason, decidedAt);

        verification.Status.ShouldBe(WorkerVerificationStatus.Rejected);
        verification.DecidedAt.ShouldBe(decidedAt);
        verification.RejectionReason!.Length.ShouldBe(WorkerVerification.MaxRejectionReasonLength);
    }

    [Fact]
    public void AllowNullRejectionReason()
    {
        var verification = new WorkerVerification(Guid.NewGuid(), DateTimeOffset.UtcNow);
        verification.MarkPending("Persona", "inq_123", DateTimeOffset.UtcNow);

        verification.MarkRejected(null, DateTimeOffset.UtcNow);

        verification.RejectionReason.ShouldBeNull();
    }
}
