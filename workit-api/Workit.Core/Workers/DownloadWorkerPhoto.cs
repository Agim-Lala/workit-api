using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Storage;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

public static class DownloadWorkerPhoto
{
    private static readonly IReadOnlyDictionary<string, string> ContentTypesByExtension = new Dictionary<string, string>
    {
        [".jpg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    public sealed record Request(Guid WorkerUserId) : IRequest<Response>;

    public sealed record Response(Stream Content, string ContentType);

    internal sealed class Handler(ReadAppDbContext db, IFileStorage fileStorage)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var exists = await db.Set<WorkerProfile>()
                .Where(workerProfile => workerProfile.UserId == request.WorkerUserId)
                .Select(workerProfile => new { HasProfile = true, workerProfile.PhotoStorageKey })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");

            if (exists.PhotoStorageKey is null)
            {
                throw new NotFoundException("error.workerPhotoNotFound");
            }

            var content = await fileStorage.OpenReadAsync(exists.PhotoStorageKey, cancellationToken);
            var contentType = ContentTypesByExtension.GetValueOrDefault(
                Path.GetExtension(exists.PhotoStorageKey),
                "application/octet-stream");

            return new Response(content, contentType);
        }
    }
}
