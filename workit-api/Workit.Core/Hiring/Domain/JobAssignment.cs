namespace Workit.Core.Hiring.Domain;

/// <summary>
/// Records that a business hired a worker for a specific job opening — the gate reviews check
/// against, the same way Airbnb/Booking only let a stay be reviewed after checkout. Reviews are
/// allowed once this reaches <see cref="AssignmentStatus.Completed"/>, one per side.
/// </summary>
public sealed class JobAssignment
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid JobOpeningId { get; private set; }
    public Guid BusinessProfileId { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public AssignmentStatus Status { get; private set; } = AssignmentStatus.Active;
    public DateTimeOffset HiredAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private JobAssignment()
    {
    }

    public JobAssignment(Guid jobOpeningId, Guid businessProfileId, Guid workerProfileId, DateTimeOffset hiredAt)
    {
        JobOpeningId = jobOpeningId;
        BusinessProfileId = businessProfileId;
        WorkerProfileId = workerProfileId;
        HiredAt = hiredAt;
    }

    /// <summary>Marks the work as done, unlocking reviews from both sides.</summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status == AssignmentStatus.Completed)
        {
            throw new InvalidOperationException("An already-completed assignment cannot be completed again.");
        }

        Status = AssignmentStatus.Completed;
        CompletedAt = now;
    }
}
