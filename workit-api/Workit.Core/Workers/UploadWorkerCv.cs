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

public static class UploadWorkerCv
{
    public const long MaxSizeInBytes = 5 * 1024 * 1024;
    public const string ContainerName = "worker-cvs";

    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes = new Dictionary<string, string>
    {
        ["application/pdf"] = ".pdf"
    };

    public sealed record Request(
        Guid WorkerUserId,
        Stream Content,
        string FileName,
        string ContentType,
        long Length) : IRequest<Response>;

    public sealed record Response(string FileName, DateTimeOffset UploadedAt);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.WorkerUserId)
                .NotEmpty();

            RuleFor(request => request.FileName)
                .NotEmpty()
                .MaximumLength(WorkerProfile.MaxOriginalFileNameLength);

            RuleFor(request => request.Length)
                .GreaterThan(0)
                .LessThanOrEqualTo(MaxSizeInBytes)
                .WithMessage($"The CV must be {MaxSizeInBytes / (1024 * 1024)}MB or smaller.");

            RuleFor(request => request.ContentType)
                .Must(AllowedContentTypes.ContainsKey)
                .WithMessage("The CV must be a PDF file.");
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

            var previousStorageKey = profile.CvStorageKey;
            var extension = AllowedContentTypes[request.ContentType];
            var storageKey = await fileStorage.SaveAsync(request.Content, ContainerName, extension, cancellationToken);

            var now = clock.UtcNow;
            profile.SetCv(storageKey, request.FileName, now);
            await dataWriter.SaveAsync(cancellationToken);

            if (previousStorageKey is not null)
            {
                await fileStorage.DeleteAsync(previousStorageKey, cancellationToken);
            }

            return new Response(profile.CvOriginalFileName!, now);
        }
    }
}
