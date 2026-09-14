using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

/// <summary>
/// Applies a Persona inquiry status update to the matching <see cref="WorkerVerification"/>.
/// Unrecognized statuses (still-in-progress states such as "pending" or "needs_review") are
/// acknowledged without changing anything — only a clear approve/decline moves the badge.
/// </summary>
public static class HandlePersonaWebhook
{
    private static readonly HashSet<string> ApprovedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "approved", "completed", "passed"
    };

    private static readonly HashSet<string> DeclinedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "declined", "failed", "expired"
    };

    public sealed record Request(string ProviderReferenceId, string Status) : IRequest<Response>;

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
                    "Received a Persona webhook for unknown inquiry {InquiryId}",
                    request.ProviderReferenceId);
                return new Response(false);
            }

            var now = clock.UtcNow;

            if (ApprovedStatuses.Contains(request.Status))
            {
                verification.MarkVerified(now);
            }
            else if (DeclinedStatuses.Contains(request.Status))
            {
                verification.MarkRejected($"Provider status: {request.Status}", now);
            }
            else
            {
                logger.LogInformation(
                    "Persona inquiry {InquiryId} reported in-progress status {Status}",
                    request.ProviderReferenceId,
                    request.Status);
                return new Response(false);
            }

            await dataWriter.SaveAsync(cancellationToken);
            return new Response(true);
        }
    }
}
