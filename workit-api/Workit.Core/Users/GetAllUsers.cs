using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Requests;
using Workit.Core.Shared.Time;
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

    internal sealed class Handler(ReadAppDbContext db, IClock clock, ILocalizer localizer)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var page = request.SafePage;
            var pageSize = request.SafePageSize;
            var query = db.Set<User>()
                .OrderBy(user => user.CreatedAt)
                .ThenBy(user => user.Id);

            var totalCount = await query.CountAsync(cancellationToken);
            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new { user.Id, user.Email, user.Role, user.EmailConfirmed, user.EmailConfirmationTokenExpiresAt })
                .ToListAsync(cancellationToken);

            var now = clock.UtcNow;
            var users = rows
                .Select(row =>
                {
                    var status = row.EmailConfirmed
                        ? EmailConfirmationStatus.Confirmed
                        : row.EmailConfirmationTokenExpiresAt.HasValue && row.EmailConfirmationTokenExpiresAt <= now
                            ? EmailConfirmationStatus.Expired
                            : EmailConfirmationStatus.Pending;

                    return new UserDto(row.Id, row.Email, row.Role, localizer.Enum(row.Role), status, localizer.Enum(status));
                })
                .ToList();

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
