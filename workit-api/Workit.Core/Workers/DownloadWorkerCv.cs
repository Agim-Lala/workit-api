using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Storage;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

public static class DownloadWorkerCv
{
    public sealed record Request(Guid WorkerUserId) : IRequest<Response>;

    public sealed record Response(Stream Content, string FileName);

    internal sealed class Handler(ReadAppDbContext db, IFileStorage fileStorage)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var profile = await db.Set<WorkerProfile>()
                .Where(workerProfile => workerProfile.UserId == request.WorkerUserId)
                .Select(workerProfile => new { workerProfile.CvStorageKey, workerProfile.CvOriginalFileName })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");

            if (profile.CvStorageKey is null)
            {
                throw new NotFoundException("error.workerCvNotFound");
            }

            var content = await fileStorage.OpenReadAsync(profile.CvStorageKey, cancellationToken);
            return new Response(content, profile.CvOriginalFileName ?? "cv.pdf");
        }
    }
}
