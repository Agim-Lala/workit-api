using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.Hiring.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Hiring;

/// <summary>Business hires a worker for one of its job openings, opening a job assignment.</summary>
public static class HireWorker
{
    public sealed record Request(Guid BusinessUserId, Guid JobOpeningId, Guid WorkerProfileId) : IRequest<Response>;

    public sealed record Response(
        Guid Id,
        Guid JobOpeningId,
        Guid BusinessProfileId,
        Guid WorkerProfileId,
        AssignmentStatus Status,
        DateTimeOffset HiredAt,
        string StatusLabel);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.BusinessUserId).NotEmpty();
            RuleFor(request => request.JobOpeningId).NotEmpty();
            RuleFor(request => request.WorkerProfileId).NotEmpty();
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

            var jobOpening = await db.Set<JobOpening>()
                .SingleOrDefaultAsync(
                    jobOpening => jobOpening.Id == request.JobOpeningId && jobOpening.BusinessProfileId == businessProfileId,
                    cancellationToken)
                ?? throw new NotFoundException("error.jobOpeningNotFound");

            var workerExists = await db.Set<WorkerProfile>()
                .AnyAsync(profile => profile.Id == request.WorkerProfileId, cancellationToken);

            if (!workerExists)
            {
                throw new NotFoundException("error.workerProfileNotFound");
            }

            var activeAssignmentsCount = await db.Set<JobAssignment>()
                .Where(assignment => assignment.JobOpeningId == jobOpening.Id && assignment.Status == AssignmentStatus.Active)
                .CountAsync(cancellationToken);

            if (activeAssignmentsCount >= jobOpening.RequiredWorkersCount)
            {
                throw new DomainException("error.jobOpeningFullyStaffed");
            }

            var alreadyAssigned = await db.Set<JobAssignment>()
                .AnyAsync(
                    assignment => assignment.JobOpeningId == jobOpening.Id
                        && assignment.WorkerProfileId == request.WorkerProfileId
                        && assignment.Status == AssignmentStatus.Active,
                    cancellationToken);

            if (alreadyAssigned)
            {
                throw new DomainException("error.workerAlreadyAssigned");
            }

            var now = clock.UtcNow;
            var assignment = new JobAssignment(jobOpening.Id, businessProfileId, request.WorkerProfileId, now);

            await dataWriter
                .Add(assignment)
                .SaveAsync(cancellationToken);

            return new Response(
                assignment.Id,
                assignment.JobOpeningId,
                assignment.BusinessProfileId,
                assignment.WorkerProfileId,
                assignment.Status,
                assignment.HiredAt,
                localizer.Enum(assignment.Status));
        }
    }
}
