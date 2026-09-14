using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

/// <summary>
/// Applies a Stripe Identity VerificationSession event to the matching
/// <see cref="WorkerVerification"/>. Other event types (still-in-progress states) are
/// acknowledged without changing anything — only a clear verified/requires_input event moves
/// the badge.
/// </summary>
public static class HandleStripeIdentityWebhook
{
    private const string VerifiedEventType = "identity.verification_session.verified";
    private const string RequiresInputEventType = "identity.verification_session.requires_input";

    public sealed record Request(string EventType, string ProviderReferenceId) : IRequest<Response>;

    public sealed record Response(bool Applied);

    internal sealed class Handler(AppDbContext db, IDataWriter dataWriter, IClock clock, ILogger<Handler> logger)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var verification = await db.Set<WorkerVerification>()
                .SingleOrDefaultAsync(v => v.ProviderReferenceId == request.ProviderReferenceId, cancellationToken);

            if (verification is null)
            {
                logger.LogWarning(
                    "Received a Stripe Identity webhook for an unknown session {SessionId}",
                    request.ProviderReferenceId);
                return new Response(false);
            }

            var now = clock.UtcNow;

            switch (request.EventType)
            {
                case VerifiedEventType:
                    verification.MarkVerified(now);
                    break;
                case RequiresInputEventType:
                    verification.MarkRejected("Stripe Identity verification requires further input.", now);
                    break;
                default:
                    logger.LogInformation(
                        "Stripe Identity session {SessionId} reported event {EventType}",
                        request.ProviderReferenceId,
                        request.EventType);
                    return new Response(false);
            }

            await dataWriter.SaveAsync(cancellationToken);
            return new Response(true);
        }
    }
}
