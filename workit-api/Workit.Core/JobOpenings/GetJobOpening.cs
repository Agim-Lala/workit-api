using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;
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
        DateTimeOffset CreatedAt,
        string Language,
        string PayTypeLabel,
        string JobTypeLabel,
        string ShiftTypeLabel,
        string StatusLabel);

    internal sealed class Handler(ReadAppDbContext db, ILocalizer localizer)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var jobOpening = await db.Set<JobOpening>()
                .SingleOrDefaultAsync(jobOpening => jobOpening.Id == request.Id, cancellationToken)
                ?? throw new NotFoundException("error.jobOpeningNotFound");

            return JobOpeningPresentation.ToResponse(jobOpening, localizer);
        }
    }
}
