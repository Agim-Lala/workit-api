namespace Workit.Core.Reviews.Domain;

/// <summary>
/// A 1-5 rating one side of a completed <see cref="Workit.Core.Hiring.Domain.JobAssignment"/>
/// leaves for the other. At most one per (assignment, reviewer role) — the unique index in
/// <c>ReviewConfiguration</c> enforces that, not this type.
/// </summary>
public sealed class Review
{
    public const int MaxCommentLength = 1000;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid JobAssignmentId { get; private set; }
    public ReviewerRole ReviewerRole { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Review()
    {
    }

    public Review(Guid jobAssignmentId, ReviewerRole reviewerRole, int rating, string? comment, DateTimeOffset createdAt)
    {
        JobAssignmentId = jobAssignmentId;
        ReviewerRole = reviewerRole;
        Rating = rating;
        Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        CreatedAt = createdAt;
    }
}
