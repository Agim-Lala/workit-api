using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Requests;

namespace Workit.Core.JobOpenings;

public static class GetBusinessJobOpenings
{
    public sealed record Request(
        Guid BusinessUserId,
        int Page = 1,
        int PageSize = 25,
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
            var businessProfileId = await db.Set<BusinessProfile>()
                .Where(profile => profile.UserId == request.BusinessUserId)
                .Select(profile => profile.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (businessProfileId == Guid.Empty)
            {
                throw new NotFoundException("error.businessProfileNotFound");
            }

            var page = request.SafePage;
            var pageSize = request.SafePageSize;
            var query = db.Set<JobOpening>()
                .Where(jobOpening => jobOpening.BusinessProfileId == businessProfileId);

            if (request.Status.HasValue)
            {
                query = query.Where(jobOpening => jobOpening.Status == request.Status);
            }

            query = query
                .OrderByDescending(jobOpening => jobOpening.CreatedAt)
                .ThenBy(jobOpening => jobOpening.Id);

            var totalCount = await query.CountAsync(cancellationToken);
            var jobOpenings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var items = jobOpenings
                .Select(jobOpening => JobOpeningPresentation.ToBusinessListItem(jobOpening, localizer))
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
