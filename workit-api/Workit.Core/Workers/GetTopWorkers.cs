using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.Hiring.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Reviews.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

/// <summary>
/// Ranks workers for one of a business's job openings by profile fit (interested fields, shift
/// preference, uploaded CV), past completed work and the average rating businesses gave them.
/// </summary>
public static class GetTopWorkers
{
    public const int MaxLimit = 50;

    public sealed record Request(Guid BusinessUserId, Guid JobOpeningId, int Limit = 20) : IRequest<Response>;

    public sealed record Response(IReadOnlyList<Item> Items);

    public sealed record Item(
        Guid WorkerProfileId,
        string FirstName,
        string LastName,
        string Location,
        bool HasCv,
        bool CvMatchesRole,
        int? CvYearsOfExperience,
        int CvLanguagesCount,
        bool MatchesInterestedFields,
        bool MatchesPreferredShiftType,
        int CompletedJobsCount,
        int CompletedSameRoleJobsCount,
        double? AverageRating,
        int ReviewsCount,
        double Score);

    internal sealed class Handler(ReadAppDbContext db) : IRequestHandler<Request, Response>
    {
        // Same flavour as GetJobOpenings: flat boosts for profile fit, saturating curves for
        // experience, rating scaled to [0, 1]. Tuned by feel, not measured.
        private const double InterestMatchWeight = 1.0;
        private const double ShiftMatchWeight = 0.5;
        private const double CvWeight = 0.25;
        private const double CvRoleMatchWeight = 1.0;
        private const double CvYearsWeight = 0.75;
        private const double CvLanguageWeight = 0.1;
        private const int MaxCountedLanguages = 4;
        private const double ExperienceWeight = 1.0;
        private const double SameRoleExperienceWeight = 1.0;
        private const double RatingWeight = 1.5;
        private const double UnratedRating = 3.0;

        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var businessProfileId = await db.Set<BusinessProfile>()
                .Where(profile => profile.UserId == request.BusinessUserId)
                .Select(profile => profile.Id)
                .SingleOrDefaultAsync(cancellationToken);

            var jobOpening = await db.Set<JobOpening>()
                .SingleOrDefaultAsync(
                    opening => opening.Id == request.JobOpeningId && opening.BusinessProfileId == businessProfileId,
                    cancellationToken)
                ?? throw new NotFoundException("error.jobOpeningNotFound");

            // ponytail: loads every worker, completed assignment and review into memory (interest
            // fields are JSON-stored, like GetJobOpenings). Move aggregates to SQL when volume grows.
            var workers = await db.Set<WorkerProfile>().ToListAsync(cancellationToken);

            var completedJobs = await (
                from assignment in db.Set<JobAssignment>()
                join opening in db.Set<JobOpening>() on assignment.JobOpeningId equals opening.Id
                where assignment.Status == AssignmentStatus.Completed
                select new { assignment.WorkerProfileId, opening.Role })
                .ToListAsync(cancellationToken);
            var experienceByWorker = completedJobs.ToLookup(job => job.WorkerProfileId);

            var ratings = await (
                from review in db.Set<Review>()
                join assignment in db.Set<JobAssignment>() on review.JobAssignmentId equals assignment.Id
                where review.ReviewerRole == ReviewerRole.Business
                select new { assignment.WorkerProfileId, review.Rating })
                .ToListAsync(cancellationToken);
            var ratingsByWorker = ratings.ToLookup(rating => rating.WorkerProfileId, rating => rating.Rating);

            var alreadyAssigned = await db.Set<JobAssignment>()
                .Where(assignment => assignment.JobOpeningId == jobOpening.Id && assignment.Status == AssignmentStatus.Active)
                .Select(assignment => assignment.WorkerProfileId)
                .ToListAsync(cancellationToken);

            var jobRoles = CvReader.RolesIn($"{jobOpening.Role} {jobOpening.Title}");
            var limit = Math.Clamp(request.Limit, 1, MaxLimit);
            var items = workers
                .Where(worker => !alreadyAssigned.Contains(worker.Id))
                .Select(worker =>
                {
                    var matchesInterestedFields = worker.InterestedFields.Any(field =>
                        jobOpening.Role.Contains(field, StringComparison.OrdinalIgnoreCase)
                        || jobOpening.Title.Contains(field, StringComparison.OrdinalIgnoreCase));
                    var matchesPreferredShiftType = worker.PreferredShiftTypes.Contains(jobOpening.ShiftType);
                    var hasCv = worker.CvStorageKey is not null;
                    // Synonym-aware when the opening's role is in CvReader's list ("Kamarier" matches a
                    // "server" CV); otherwise falls back to a plain text search of the CV.
                    var cvMatchesRole = jobRoles.Count > 0
                        ? worker.CvRoles.Intersect(jobRoles).Any()
                        : worker.CvText?.Contains(jobOpening.Role, StringComparison.OrdinalIgnoreCase) == true;
                    var cvYears = worker.CvYearsOfExperience ?? 0;
                    var jobs = experienceByWorker[worker.Id].ToList();
                    var sameRoleJobs = jobs.Count(job => string.Equals(job.Role, jobOpening.Role, StringComparison.OrdinalIgnoreCase));
                    var workerRatings = ratingsByWorker[worker.Id].ToList();
                    double? averageRating = workerRatings.Count == 0 ? null : workerRatings.Average();

                    var score = (matchesInterestedFields ? InterestMatchWeight : 0.0)
                        + (matchesPreferredShiftType ? ShiftMatchWeight : 0.0)
                        + (hasCv ? CvWeight : 0.0)
                        + (cvMatchesRole ? CvRoleMatchWeight : 0.0)
                        + CvYearsWeight * cvYears / (cvYears + 3.0)
                        + CvLanguageWeight * Math.Min(worker.CvLanguages.Count, MaxCountedLanguages)
                        + ExperienceWeight * Saturate(jobs.Count)
                        + SameRoleExperienceWeight * Saturate(sameRoleJobs)
                        + RatingWeight * (averageRating ?? UnratedRating) / 5.0;

                    return new Item(
                        worker.Id,
                        worker.FirstName,
                        worker.LastName,
                        worker.Location,
                        hasCv,
                        cvMatchesRole,
                        worker.CvYearsOfExperience,
                        worker.CvLanguages.Count,
                        matchesInterestedFields,
                        matchesPreferredShiftType,
                        jobs.Count,
                        sameRoleJobs,
                        averageRating,
                        workerRatings.Count,
                        Math.Round(score, 3));
                })
                .OrderByDescending(item => item.Score)
                .ThenBy(item => item.WorkerProfileId)
                .Take(limit)
                .ToList();

            return new Response(items);
        }

        // 0 jobs -> 0, 1 -> 0.5, 3 -> 0.75: more experience helps with diminishing returns.
        private static double Saturate(int count) => count / (count + 1.0);
    }
}
