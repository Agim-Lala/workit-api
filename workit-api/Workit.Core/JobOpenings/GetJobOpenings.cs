using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Requests;
using Workit.Core.Workers.Domain;

namespace Workit.Core.JobOpenings;

public static class GetJobOpenings
{
    public sealed record Request(
        Guid WorkerUserId,
        int Page = 1,
        int PageSize = 25,
        Guid? BusinessProfileId = null,
        JobOpeningStatus? Status = null,
        JobType? JobType = null,
        DateOnly? OnDate = null,
        ShiftType? ShiftType = null)
        : PagedRequest(Page, PageSize), IRequest<Response>;

    public sealed record Response(
        IReadOnlyList<Item> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPreviousPage,
        bool HasNextPage);

    public sealed record Item(
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
        string PayTypeLabel,
        string JobTypeLabel,
        string ShiftTypeLabel,
        string StatusLabel);

    internal sealed class Handler(ReadAppDbContext db, ILocalizer localizer)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var page = request.SafePage;
            var pageSize = request.SafePageSize;
            var workerLocation = await db.Set<WorkerProfile>()
                .Where(profile => profile.UserId == request.WorkerUserId)
                .Select(profile => profile.Location)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");
            var query = db.Set<JobOpening>().AsQueryable();

            if (!string.IsNullOrWhiteSpace(workerLocation))
            {
                var normalizedLocation = workerLocation.Trim().ToLower();
                query = query.Where(jobOpening =>
                    jobOpening.Location.ToLower().Contains(normalizedLocation));
            }

            if (request.BusinessProfileId.HasValue)
            {
                query = query.Where(jobOpening => jobOpening.BusinessProfileId == request.BusinessProfileId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(jobOpening => jobOpening.Status == request.Status);
            }

            if (request.JobType.HasValue)
            {
                query = query.Where(jobOpening => jobOpening.JobType == request.JobType);
            }

            if (request.OnDate.HasValue)
            {
                var onDate = request.OnDate.Value;
                query = query.Where(jobOpening =>
                    jobOpening.StartDate <= onDate
                    && (!jobOpening.EndDate.HasValue || jobOpening.EndDate >= onDate));
            }

            if (request.ShiftType.HasValue)
            {
                query = query.Where(jobOpening => jobOpening.ShiftType == request.ShiftType);
            }

            query = query
                .OrderBy(jobOpening => jobOpening.StartDate)
                .ThenBy(jobOpening => jobOpening.ShiftStartTime)
                .ThenBy(jobOpening => jobOpening.Id);

            var totalCount = await query.CountAsync(cancellationToken);
            var jobOpenings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var items = jobOpenings
                .Select(jobOpening => JobOpeningPresentation.ToListItem(jobOpening, localizer))
                .ToList();

            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new Response(
                items,
                page,
                pageSize,
                totalCount,
                totalPages,
                page > 1,
                totalPages > page);
        }
    }
}
