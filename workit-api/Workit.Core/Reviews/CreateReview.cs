using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.Hiring.Domain;
using Workit.Core.Reviews.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Reviews;

/// <summary>
/// Either side of a completed <see cref="JobAssignment"/> rates and reviews the other — same gate
/// as Airbnb/Booking: no review until the stay/job is over, and only its two participants may
/// leave one, at most once each.
/// </summary>
public static class CreateReview
{
    public sealed record Request(Guid CallerUserId, Guid JobAssignmentId, int Rating, string? Comment) : IRequest<Response>;

    public sealed record Response(
        Guid Id,
        Guid JobAssignmentId,
        ReviewerRole ReviewerRole,
        int Rating,
        string? Comment,
        DateTimeOffset CreatedAt);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.CallerUserId).NotEmpty();
            RuleFor(request => request.JobAssignmentId).NotEmpty();
            RuleFor(request => request.Rating).InclusiveBetween(1, 5);
            RuleFor(request => request.Comment!)
                .MaximumLength(Review.MaxCommentLength)
                .When(request => request.Comment is not null);
        }
    }

    internal sealed class Handler(AppDbContext db, IDataWriter dataWriter, IClock clock)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var assignment = await db.Set<JobAssignment>()
                .SingleOrDefaultAsync(assignment => assignment.Id == request.JobAssignmentId, cancellationToken)
                ?? throw new NotFoundException("error.jobAssignmentNotFound");

            var reviewerRole = await ResolveReviewerRoleAsync(db, request.CallerUserId, assignment, cancellationToken)
                ?? throw new DomainException("error.notAssignmentParticipant");

            if (assignment.Status != AssignmentStatus.Completed)
            {
                throw new DomainException("error.assignmentNotCompleted");
            }

            var alreadyReviewed = await db.Set<Review>()
                .AnyAsync(
                    review => review.JobAssignmentId == assignment.Id && review.ReviewerRole == reviewerRole,
                    cancellationToken);

            if (alreadyReviewed)
            {
                throw new DomainException("error.reviewAlreadySubmitted");
            }

            var review = new Review(assignment.Id, reviewerRole, request.Rating, request.Comment, clock.UtcNow);

            await dataWriter
                .Add(review)
                .SaveAsync(cancellationToken);

            return new Response(
                review.Id,
                review.JobAssignmentId,
                review.ReviewerRole,
                review.Rating,
                review.Comment,
                review.CreatedAt);
        }

        private static async Task<ReviewerRole?> ResolveReviewerRoleAsync(
            AppDbContext db,
            Guid callerUserId,
            JobAssignment assignment,
            CancellationToken cancellationToken)
        {
            var isTheHiredWorker = await db.Set<WorkerProfile>()
                .AnyAsync(
                    profile => profile.UserId == callerUserId && profile.Id == assignment.WorkerProfileId,
                    cancellationToken);

            if (isTheHiredWorker)
            {
                return ReviewerRole.Worker;
            }

            var isTheHiringBusiness = await db.Set<BusinessProfile>()
                .AnyAsync(
                    profile => profile.UserId == callerUserId && profile.Id == assignment.BusinessProfileId,
                    cancellationToken);

            return isTheHiringBusiness ? ReviewerRole.Business : null;
        }
    }
}
