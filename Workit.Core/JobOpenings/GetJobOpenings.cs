using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Requests;

namespace Workit.Core.JobOpenings;

public static class GetJobOpenings
{
    public sealed record Request(
        int Page = 1,
        int PageSize = 25,
        Guid? BusinessProfileId = null,
        JobOpeningStatus? Status = null)
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
        JobScheduleType ScheduleType,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int RequiredWorkersCount,
        JobOpeningStatus Status,
        DateTimeOffset CreatedAt);

    internal sealed class Handler(ReadAppDbContext db) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var page = request.SafePage;
            var pageSize = request.SafePageSize;
            var query = db.Set<JobOpening>().AsQueryable();

            if (request.BusinessProfileId.HasValue)
            {
                query = query.Where(jobOpening => jobOpening.BusinessProfileId == request.BusinessProfileId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(jobOpening => jobOpening.Status == request.Status);
            }

            query = query
                .OrderBy(jobOpening => jobOpening.StartsAt)
                .ThenBy(jobOpening => jobOpening.Id);

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(jobOpening => new Item(
                    jobOpening.Id,
                    jobOpening.BusinessProfileId,
                    jobOpening.Title,
                    jobOpening.Description,
                    jobOpening.Role,
                    jobOpening.Location,
                    jobOpening.PayAmount,
                    jobOpening.PayType,
                    jobOpening.ScheduleType,
                    jobOpening.StartsAt,
                    jobOpening.EndsAt,
                    jobOpening.RequiredWorkersCount,
                    jobOpening.Status,
                    jobOpening.CreatedAt))
                .ToListAsync(cancellationToken);

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
