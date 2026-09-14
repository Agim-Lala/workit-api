using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.IdentityVerification;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

/// <summary>Starts a hosted identity-verification session for the worker's ID-verification badge.</summary>
public static class StartWorkerVerification
{
    public sealed record Request(Guid WorkerUserId) : IRequest<Response>;

    public sealed record Response(string HostedUrl, WorkerVerificationStatus Status);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.WorkerUserId).NotEmpty();
        }
    }

    internal sealed class Handler(
        AppDbContext db,
        IDataWriter dataWriter,
        IIdentityVerificationProvider identityVerificationProvider,
        IClock clock) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var profile = await db.Set<WorkerProfile>()
                .SingleOrDefaultAsync(
                    workerProfile => workerProfile.UserId == request.WorkerUserId,
                    cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");

            var verification = await db.Set<WorkerVerification>()
                .SingleOrDefaultAsync(v => v.WorkerProfileId == profile.Id, cancellationToken);

            if (verification?.Status == WorkerVerificationStatus.Verified)
            {
                throw new DomainException("error.workerAlreadyVerified");
            }

            var session = await identityVerificationProvider.StartAsync(profile.Id, cancellationToken);
            var now = clock.UtcNow;

            if (verification is null)
            {
                verification = new WorkerVerification(profile.Id, now);
                dataWriter.Add(verification);
            }

            verification.MarkPending(identityVerificationProvider.ProviderName, session.ProviderReferenceId, now);
            await dataWriter.SaveAsync(cancellationToken);

            return new Response(session.HostedUrl, verification.Status);
        }
    }
}
