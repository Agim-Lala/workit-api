using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;
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
        string Location,
        bool IsLocationVerified,
        string? Country,
        bool HasCv,
        string? CvFileName,
        DateTimeOffset? CvUploadedAt,
        bool HasPhoto,
        DateTimeOffset? PhotoUploadedAt,
        IReadOnlyList<string> InterestedFields,
        IReadOnlyList<ShiftType> PreferredShiftTypes,
        IReadOnlyList<string> PreferredShiftTypeLabels,
        WorkerVerificationStatus VerificationStatus,
        string VerificationStatusLabel);

    internal sealed class Handler(ReadAppDbContext db, ILocalizer localizer) : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var row = await (
                    from candidate in db.Set<WorkerProfile>()
                    where candidate.UserId == request.WorkerUserId
                    join verification in db.Set<WorkerVerification>()
                        on candidate.Id equals verification.WorkerProfileId into verifications
                    from verification in verifications.DefaultIfEmpty()
                    select new { Profile = candidate, Verification = verification })
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");

            var profile = row.Profile;
            var verificationStatus = row.Verification?.Status ?? WorkerVerificationStatus.NotStarted;

            return new Response(
                profile.Id,
                profile.UserId,
                profile.FirstName,
                profile.LastName,
                profile.Phone,
                profile.Location,
                profile.IsLocationVerified,
                profile.Country,
                profile.CvStorageKey is not null,
                profile.CvOriginalFileName,
                profile.CvUploadedAt,
                profile.PhotoStorageKey is not null,
                profile.PhotoUploadedAt,
                profile.InterestedFields,
                profile.PreferredShiftTypes,
                profile.PreferredShiftTypes.Select(localizer.Enum).ToList(),
                verificationStatus,
                localizer.Enum(verificationStatus));
        }
    }
}
