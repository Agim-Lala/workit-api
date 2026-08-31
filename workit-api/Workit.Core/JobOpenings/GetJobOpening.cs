using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;

namespace Workit.Core.JobOpenings;

public static class GetJobOpening
{
    public sealed record Request(Guid Id) : IRequest<Response>;

    public sealed record Response(
        Guid Id,
        Guid BusinessProfileId,
        string Title,
        string Description,
        string Role,
        string Location,
        decimal PayAmount,
        PayType PayType,
        JobType JobType,
        DateOnly StartDate,
        DateOnly? EndDate,
        ShiftType ShiftType,
        TimeOnly? ShiftStartTime,
        TimeOnly? ShiftEndTime,
        int RequiredWorkersCount,
        JobOpeningStatus Status,
        DateTimeOffset CreatedAt);

    internal sealed class Handler(ReadAppDbContext db) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var response = await db.Set<JobOpening>()
                .Where(jobOpening => jobOpening.Id == request.Id)
                .Select(jobOpening => new Response(
                    jobOpening.Id,
                    jobOpening.BusinessProfileId,
                    jobOpening.Title,
                    jobOpening.Description,
                    jobOpening.Role,
                    jobOpening.Location,
                    jobOpening.PayAmount,
                    jobOpening.PayType,
                    jobOpening.JobType,
                    jobOpening.StartDate,
                    jobOpening.EndDate,
                    jobOpening.ShiftType,
                    jobOpening.ShiftStartTime,
                    jobOpening.ShiftEndTime,
                    jobOpening.RequiredWorkersCount,
                    jobOpening.Status,
                    jobOpening.CreatedAt))
                .SingleOrDefaultAsync(cancellationToken);

            return response ?? throw new NotFoundException("Job opening not found.");
        }
    }
}
