using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.Hiring.Domain;
using Workit.Core.Reviews.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Requests;

namespace Workit.Core.Reviews;

/// <summary>Reviews a business received from workers after completed job assignments.</summary>
public static class GetBusinessReviews
{
    public sealed record Request(Guid BusinessProfileId, int Page = 1, int PageSize = 25)
        : PagedRequest(Page, PageSize), IRequest<Response>;

    public sealed record Response(
        IReadOnlyList<Item> Items,
        double? AverageRating,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPreviousPage,
        bool HasNextPage);

    public sealed record Item(Guid Id, Guid JobAssignmentId, int Rating, string? Comment, DateTimeOffset CreatedAt);

    internal sealed class Handler(ReadAppDbContext db) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var businessExists = await db.Set<BusinessProfile>()
                .AnyAsync(profile => profile.Id == request.BusinessProfileId, cancellationToken);

            if (!businessExists)
            {
                throw new NotFoundException("error.businessProfileNotFound");
            }

            var page = request.SafePage;
            var pageSize = request.SafePageSize;
            var query =
                from review in db.Set<Review>()
                join assignment in db.Set<JobAssignment>() on review.JobAssignmentId equals assignment.Id
                where assignment.BusinessProfileId == request.BusinessProfileId && review.ReviewerRole == ReviewerRole.Worker
                orderby review.CreatedAt descending, review.Id
                select review;

            var totalCount = await query.CountAsync(cancellationToken);
            var averageRating = totalCount == 0 ? null : (double?)await query.AverageAsync(review => review.Rating, cancellationToken);
            var reviews = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
            var items = reviews
                .Select(review => new Item(review.Id, review.JobAssignmentId, review.Rating, review.Comment, review.CreatedAt))
                .ToList();

            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new Response(
                items,
                averageRating,
                page,
                pageSize,
                totalCount,
                totalPages,
                page > 1,
                totalPages > page);
        }
    }
}
