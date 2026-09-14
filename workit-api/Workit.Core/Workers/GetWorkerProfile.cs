using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

public static class GetWorkerProfile
{
    public sealed record Request(Guid WorkerUserId) : IRequest<Response>;

    public sealed record Response(
        Guid Id,
        Guid UserId,
        string FirstName,
        string LastName,
        string? Phone,
        string Location);

    internal sealed class Handler(ReadAppDbContext db) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            return await db.Set<WorkerProfile>()
                .Where(profile => profile.UserId == request.WorkerUserId)
                .Select(profile => new Response(
                    profile.Id,
                    profile.UserId,
                    profile.FirstName,
                    profile.LastName,
                    profile.Phone,
                    profile.Location))
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");
        }
    }
}
