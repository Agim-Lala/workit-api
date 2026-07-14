using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Requests;
using Workit.Core.Users.Domain;
using Workit.Core.Users.Shared;

namespace Workit.Core.Users;

public static class GetAllUsers
{
    public sealed record Request(int Page = 1, int PageSize = 25) : PagedRequest(Page, PageSize), IRequest<Response>;

    public sealed record Response(
        IReadOnlyList<UserDto> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPreviousPage,
        bool HasNextPage);

    internal sealed class Handler(ReadAppDbContext db) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var page = request.SafePage;
            var pageSize = request.SafePageSize;
            var query = db.Set<User>()
                .OrderBy(user => user.CreatedAt)
                .ThenBy(user => user.Id);

            var totalCount = await query.CountAsync(cancellationToken);
            var users = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new UserDto(user.Id, user.Email, user.Role))
                .ToListAsync(cancellationToken);

            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new Response(
                users,
                page,
                pageSize,
                totalCount,
                totalPages,
                page > 1,
                totalPages > page);
        }
    }
}
