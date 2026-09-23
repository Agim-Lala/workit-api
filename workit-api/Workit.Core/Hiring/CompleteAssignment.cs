using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.Hiring.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;

namespace Workit.Core.Hiring;

/// <summary>Business marks a job assignment as done, unlocking reviews from both sides.</summary>
public static class CompleteAssignment
{
    public sealed record Request(Guid BusinessUserId, Guid JobAssignmentId) : IRequest<Response>;

    public sealed record Response(
        Guid Id,
        AssignmentStatus Status,
        DateTimeOffset? CompletedAt,
        string StatusLabel);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.BusinessUserId).NotEmpty();
            RuleFor(request => request.JobAssignmentId).NotEmpty();
        }
    }

    internal sealed class Handler(AppDbContext db, IDataWriter dataWriter, IClock clock, ILocalizer localizer)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var businessProfileId = await db.Set<BusinessProfile>()
                .Where(profile => profile.UserId == request.BusinessUserId)
                .Select(profile => profile.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (businessProfileId == Guid.Empty)
            {
                throw new NotFoundException("error.businessProfileNotFound");
            }

            var assignment = await db.Set<JobAssignment>()
                .SingleOrDefaultAsync(
                    assignment => assignment.Id == request.JobAssignmentId && assignment.BusinessProfileId == businessProfileId,
                    cancellationToken)
                ?? throw new NotFoundException("error.jobAssignmentNotFound");

            if (assignment.Status == AssignmentStatus.Completed)
            {
                throw new DomainException("error.assignmentAlreadyCompleted");
            }

            assignment.Complete(clock.UtcNow);
            await dataWriter.SaveAsync(cancellationToken);

            return new Response(assignment.Id, assignment.Status, assignment.CompletedAt, localizer.Enum(assignment.Status));
        }
    }
}
