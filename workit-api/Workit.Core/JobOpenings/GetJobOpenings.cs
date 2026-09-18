using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Requests;
using Workit.Core.Workers.Domain;

namespace Workit.Core.JobOpenings;

public static class GetJobOpenings
{
    public sealed record Request(
        Guid WorkerUserId,
        int Page = 1,
        int PageSize = 25,
        Guid? BusinessProfileId = null,
        JobOpeningStatus? Status = null,
        JobType? JobType = null,
        DateOnly? OnDate = null,
        ShiftType? ShiftType = null)
        : PagedRequest(Page, PageSize), IRequest<Response>;

    public sealed record Response(
        IReadOnlyList<Item> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages,
        bool HasPreviousPage,
        bool HasNextPage);

    public sealed record Item(
        Guid Id,
        Guid BusinessProfileId,
        string Title,
        string Description,
        string Role,
        string Location,
        decimal PayAmount,
        PayType PayType,
        JobType JobType,
        DateOnly StartDate,
        DateOnly? EndDate,
        ShiftType ShiftType,
        TimeOnly? ShiftStartTime,
        TimeOnly? ShiftEndTime,
        int RequiredWorkersCount,
        JobOpeningStatus Status,
        DateTimeOffset CreatedAt,
        string PayTypeLabel,
        string JobTypeLabel,
        string ShiftTypeLabel,
        string StatusLabel,
        double? DistanceKm,
        bool MatchesInterestedFields,
        bool MatchesPreferredShiftType);

    internal sealed class Handler(ReadAppDbContext db, ILocalizer localizer)
        : IRequestHandler<Request, Response>
    {
        // Weights for combining match signals into one ranking score. Proximity is continuous
        // (0, 1], interest/shift matches are flat boosts, so interest is worth roughly "one full
        // proximity point" and shift preference half that — tuned by feel, not measured.
        private const double InterestMatchWeight = 1.0;
        private const double ShiftMatchWeight = 0.5;
        private const double EarthRadiusKm = 6371.0;

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var page = request.SafePage;
            var pageSize = request.SafePageSize;
            var workerProfile = await db.Set<WorkerProfile>()
                .Where(profile => profile.UserId == request.WorkerUserId)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");

            var query =
                from jobOpening in db.Set<JobOpening>()
                join businessProfile in db.Set<BusinessProfile>()
                    on jobOpening.BusinessProfileId equals businessProfile.Id
                select new { jobOpening, businessProfile };

            var hasWorkerCoordinates = workerProfile.IsLocationVerified
                && workerProfile.Latitude.HasValue
                && workerProfile.Longitude.HasValue;

            // A geo-verified worker location lets us rank by real distance instead, so every
            // job stays in the candidate set and merely sorts lower the farther away it is.
            if (!hasWorkerCoordinates && !string.IsNullOrWhiteSpace(workerProfile.Location))
            {
                var normalizedLocation = workerProfile.Location.Trim().ToLower();
                query = query.Where(candidate =>
                    candidate.jobOpening.Location.ToLower().Contains(normalizedLocation));
            }

            if (request.BusinessProfileId.HasValue)
            {
                query = query.Where(candidate => candidate.jobOpening.BusinessProfileId == request.BusinessProfileId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(candidate => candidate.jobOpening.Status == request.Status);
            }

            if (request.JobType.HasValue)
            {
                query = query.Where(candidate => candidate.jobOpening.JobType == request.JobType);
            }

            if (request.OnDate.HasValue)
            {
                var onDate = request.OnDate.Value;
                query = query.Where(candidate =>
                    candidate.jobOpening.StartDate <= onDate
                    && (!candidate.jobOpening.EndDate.HasValue || candidate.jobOpening.EndDate >= onDate));
            }

            if (request.ShiftType.HasValue)
            {
                query = query.Where(candidate => candidate.jobOpening.ShiftType == request.ShiftType);
            }

            // Match scoring blends free-text interest keywords and JSON-stored shift preferences
            // that don't translate cleanly to SQL, so candidates are scored and ranked in memory.
            // Fine at today's job-opening volume; revisit with DB-side scoring if that changes.
            var candidates = await query.ToListAsync(cancellationToken);
            var scored = candidates
                .Select(candidate =>
                {
                    var distanceKm = hasWorkerCoordinates
                        ? DistanceKm(
                            workerProfile.Latitude!.Value,
                            workerProfile.Longitude!.Value,
                            (double)candidate.businessProfile.Latitude,
                            (double)candidate.businessProfile.Longitude)
                        : (double?)null;
                    var matchesInterestedFields = workerProfile.InterestedFields.Count > 0
                        && workerProfile.InterestedFields.Any(field =>
                            candidate.jobOpening.Role.Contains(field, StringComparison.OrdinalIgnoreCase)
                            || candidate.jobOpening.Title.Contains(field, StringComparison.OrdinalIgnoreCase));
                    var matchesPreferredShiftType = workerProfile.PreferredShiftTypes.Count > 0
                        && workerProfile.PreferredShiftTypes.Contains(candidate.jobOpening.ShiftType);
                    var proximityScore = distanceKm.HasValue ? 1.0 / (1.0 + distanceKm.Value / 10.0) : 0.5;
                    var score = proximityScore
                        + (matchesInterestedFields ? InterestMatchWeight : 0.0)
                        + (matchesPreferredShiftType ? ShiftMatchWeight : 0.0);

                    return new
                    {
                        candidate.jobOpening,
                        Score = score,
                        DistanceKm = distanceKm,
                        MatchesInterestedFields = matchesInterestedFields,
                        MatchesPreferredShiftType = matchesPreferredShiftType
                    };
                })
                .OrderByDescending(scoredCandidate => scoredCandidate.Score)
                .ThenBy(scoredCandidate => scoredCandidate.jobOpening.StartDate)
                .ThenBy(scoredCandidate => scoredCandidate.jobOpening.ShiftStartTime)
                .ThenBy(scoredCandidate => scoredCandidate.jobOpening.Id)
                .ToList();

            var totalCount = scored.Count;
            var totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)pageSize);
            var items = scored
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(scoredCandidate => JobOpeningPresentation.ToListItem(
                    scoredCandidate.jobOpening,
                    localizer,
                    scoredCandidate.DistanceKm,
                    scoredCandidate.MatchesInterestedFields,
                    scoredCandidate.MatchesPreferredShiftType))
                .ToList();

            return new Response(
                items,
                page,
                pageSize,
                totalCount,
                totalPages,
                page > 1,
                totalPages > page);
        }

        private static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
    }
}
