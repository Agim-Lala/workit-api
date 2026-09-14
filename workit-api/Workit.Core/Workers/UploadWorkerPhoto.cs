using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Storage;
using Workit.Core.Shared.Time;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

public static class UploadWorkerPhoto
{
    public const long MaxSizeInBytes = 5 * 1024 * 1024;
    public const string ContainerName = "worker-photos";

    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    public sealed record Request(
        Guid WorkerUserId,
        Stream Content,
        string ContentType,
        long Length) : IRequest<Response>;

    public sealed record Response(DateTimeOffset UploadedAt);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.WorkerUserId)
                .NotEmpty();

            RuleFor(request => request.Length)
                .GreaterThan(0)
                .LessThanOrEqualTo(MaxSizeInBytes)
                .WithMessage($"The photo must be {MaxSizeInBytes / (1024 * 1024)}MB or smaller.");

            RuleFor(request => request.ContentType)
                .Must(AllowedContentTypes.ContainsKey)
                .WithMessage("The photo must be a JPEG, PNG, or WebP image.");
        }
    }

    internal sealed class Handler(
        AppDbContext db,
        IDataWriter dataWriter,
        IFileStorage fileStorage,
        IClock clock) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var profile = await db.Set<WorkerProfile>()
                .SingleOrDefaultAsync(
                    workerProfile => workerProfile.UserId == request.WorkerUserId,
                    cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");

            var previousStorageKey = profile.PhotoStorageKey;
            var extension = AllowedContentTypes[request.ContentType];
            var storageKey = await fileStorage.SaveAsync(request.Content, ContainerName, extension, cancellationToken);

            var now = clock.UtcNow;
            profile.SetPhoto(storageKey, now);
            await dataWriter.SaveAsync(cancellationToken);

            if (previousStorageKey is not null)
            {
                await fileStorage.DeleteAsync(previousStorageKey, cancellationToken);
            }

            return new Response(now);
        }
    }
}
