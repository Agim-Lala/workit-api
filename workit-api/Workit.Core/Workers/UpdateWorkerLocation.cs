using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

public static class UpdateWorkerLocation
{
    public sealed record Request(Guid WorkerUserId, string Location) : IRequest<Response>;

    public sealed record Response(string Location);

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

    internal sealed class Handler(AppDbContext db, IDataWriter dataWriter)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var profile = await db.Set<WorkerProfile>()
                .SingleOrDefaultAsync(
                    workerProfile => workerProfile.UserId == request.WorkerUserId,
                    cancellationToken)
                ?? throw new NotFoundException("Worker profile not found.");

            profile.ChangeLocation(request.Location);
            await dataWriter.SaveAsync(cancellationToken);

            return new Response(profile.Location);
        }
    }
}
