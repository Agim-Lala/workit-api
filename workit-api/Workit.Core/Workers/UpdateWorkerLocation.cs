using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Location;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

public static class UpdateWorkerLocation
{
    public sealed record Request(Guid WorkerUserId, string Location) : IRequest<Response>;

    public sealed record Response(string Location, bool IsLocationVerified, string? Country);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.WorkerUserId)
                .NotEmpty();

            RuleFor(request => request.Location)
                .NotEmpty()
                .MaximumLength(WorkerProfile.MaxLocationLength);
        }
    }

    internal sealed class Handler(AppDbContext db, IDataWriter dataWriter, ICityLookupService cityLookupService)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var profile = await db.Set<WorkerProfile>()
                .SingleOrDefaultAsync(
                    workerProfile => workerProfile.UserId == request.WorkerUserId,
                    cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");

            // A lookup miss or a third-party outage both fall back to the raw text, unverified —
            // see ICityLookupService for why this stays a soft check rather than a hard reject.
            var match = await cityLookupService.FindAsync(request.Location, cancellationToken);
            if (match is not null)
            {
                profile.VerifyLocation(match.City, match.Country, match.Latitude, match.Longitude);
            }
            else
            {
                profile.ChangeLocation(request.Location);
            }

            await dataWriter.SaveAsync(cancellationToken);

            return new Response(profile.Location, profile.IsLocationVerified, profile.Country);
        }
    }
}
