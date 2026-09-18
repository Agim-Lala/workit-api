using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Time;
using Workit.Core.Users.Domain;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Shared.Persistence.DataSeeders;

public sealed class WorkitDataSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IClock clock)
    : IDataSeeder
{
    private const string SeedPassword = "Test@1234";
    private const string WorkerEmail = "worker@workit.al";
    private const string LegacyWorkerEmail = "user@workit.al";
    private const string AdminEmail = "admin@workit.al";
    private const string BusinessEmail = "business@workit.al";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedWorkerAsync(cancellationToken);
        await SeedUserAsync(AdminEmail, UserRole.Admin, cancellationToken);
        await SeedBusinessWithJobOpeningsAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedWorkerAsync(CancellationToken cancellationToken)
    {
        var workerUser = await FindUserAsync(WorkerEmail, cancellationToken)
            ?? await FindUserAsync(LegacyWorkerEmail, cancellationToken)
            ?? await SeedUserAsync(WorkerEmail, UserRole.Worker, cancellationToken);

        var workerProfileExists = await db.Set<WorkerProfile>()
            .AnyAsync(profile => profile.UserId == workerUser.Id, cancellationToken);

        if (workerProfileExists)
        {
            return;
        }

        db.Set<WorkerProfile>().Add(new WorkerProfile(
            workerUser.Id,
            "Seed",
            "Worker",
            "Tirana",
            clock.UtcNow,
            "+355 69 000 0001"));
    }

    private async Task<User> SeedUserAsync(
        string email,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var user = await FindUserAsync(email, cancellationToken);

        if (user is not null)
        {
            return user;
        }

        user = new User(
            email,
            passwordHasher.Hash(SeedPassword),
            clock.UtcNow,
            role);
        user.ConfirmEmail();

        db.Set<User>().Add(user);
        return user;
    }

    private async Task<User?> FindUserAsync(string email, CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        return await db.Set<User>()
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);
    }

    private async Task SeedBusinessWithJobOpeningsAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var businessUser = await SeedUserAsync(BusinessEmail, UserRole.Business, cancellationToken);
        var businessProfile = await db.Set<BusinessProfile>()
            .SingleOrDefaultAsync(profile => profile.UserId == businessUser.Id, cancellationToken);

        if (businessProfile is null)
        {
            businessProfile = new BusinessProfile(
                businessUser.Id,
                "Tirana Events Group",
                "Bulevardi Deshmoret e Kombit, Tirana",
                41.3275m,
                19.8187m,
                "K12312345L",
                now,
                "+355 69 000 0000");

            db.Set<BusinessProfile>().Add(businessProfile);
        }

        var existingTitles = await db.Set<JobOpening>()
            .Where(jobOpening => jobOpening.BusinessProfileId == businessProfile.Id)
            .Select(jobOpening => jobOpening.Title)
            .ToHashSetAsync(cancellationToken);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var seeds = CreateJobOpeningSeeds(today);

        for (var index = 0; index < seeds.Count; index++)
        {
            var seed = seeds[index];
            if (existingTitles.Contains(seed.Title))
            {
                continue;
            }

            db.Set<JobOpening>().Add(new JobOpening(
                businessProfile.Id,
                seed.Title,
                seed.Description,
                seed.Role,
                seed.Location,
                seed.PayAmount,
                seed.PayType,
                seed.JobType,
                seed.StartDate,
                seed.EndDate,
                seed.ShiftType,
                seed.ShiftStartTime,
                seed.ShiftEndTime,
                seed.RequiredWorkersCount,
                now.AddMinutes(-index),
                Language.English,
                seed.Translations));
        }
    }

    private static IReadOnlyList<JobOpeningSeed> CreateJobOpeningSeeds(DateOnly today)
    {
        return
        [
            new JobOpeningSeed(
                "Friday Dinner Waiter",
                "Serve guests during a busy Friday dinner service and help reset tables between reservations.",
                "Waiter",
                "Blloku, Tirana",
                650m,
                PayType.Hourly,
                JobType.ShortTerm,
                today.AddDays(2),
                today.AddDays(2),
                ShiftType.CustomHours,
                new TimeOnly(16, 0),
                new TimeOnly(22, 0),
                3,
                new Dictionary<string, JobOpeningTranslation>
                {
                    [Language.Albanian] = new(
                        "Kamarier për darkën e së premtes",
                        "Shërbe klientët gjatë një darke të ngarkuar të së premtes dhe ndihmo në rregullimin e tavolinave mes rezervimeve.",
                        "Kamarier"),
                }),
            new JobOpeningSeed(
                "Permanent Breakfast Waiter",
                "Join the permanent breakfast team, prepare the dining room, and provide attentive morning service.",
                "Waiter",
                "Skanderbeg Square, Tirana",
                95_000m,
                PayType.Monthly,
                JobType.Permanent,
                today.AddDays(5),
                null,
                ShiftType.Morning,
                null,
                null,
                2,
                new Dictionary<string, JobOpeningTranslation>
                {
                    [Language.Albanian] = new(
                        "Kamarier i përhershëm për mëngjesin",
                        "Bashkohu me ekipin e përhershëm të mëngjesit, përgatit sallën e ngrënies dhe ofro shërbim të kujdesshëm në mëngjes.",
                        "Kamarier"),
                }),
            new JobOpeningSeed(
                "Restaurant Renovation Crew",
                "Support a two-week restaurant renovation with furniture assembly, painting, and final site preparation.",
                "General Worker",
                "Komuna e Parisit, Tirana",
                160_000m,
                PayType.Fixed,
                JobType.Project,
                today.AddDays(9),
                today.AddDays(22),
                ShiftType.Morning,
                null,
                null,
                5),
            new JobOpeningSeed(
                "Two-Day Festival Crew",
                "Support event setup, guest check-in, and floor operations across a two-day weekend festival.",
                "Event Staff",
                "Mother Teresa Square, Tirana",
                5_500m,
                PayType.Daily,
                JobType.ShortTerm,
                today.AddDays(4),
                today.AddDays(5),
                ShiftType.Evening,
                null,
                null,
                8),
            new JobOpeningSeed(
                "Overnight Hotel Receptionist",
                "Cover the hotel reception overnight, assist late arrivals, and prepare the morning handover report.",
                "Receptionist",
                "Durrës Beach, Durrës",
                700m,
                PayType.Hourly,
                JobType.ShortTerm,
                today.AddDays(7),
                today.AddDays(7),
                ShiftType.CustomHours,
                new TimeOnly(22, 0),
                new TimeOnly(6, 0),
                1),
            new JobOpeningSeed(
                "Summer Campaign Promoter",
                "Represent a summer campaign at public events, explain the offer clearly, and collect visitor feedback.",
                "Brand Promoter",
                "Shëngjin Promenade, Lezhë",
                80_000m,
                PayType.Fixed,
                JobType.Project,
                today.AddDays(14),
                today.AddDays(44),
                ShiftType.Evening,
                null,
                null,
                6),
            new JobOpeningSeed(
                "Morning Bakery Assistant",
                "Prepare displays, package fresh products, serve early customers, and keep the counter organized.",
                "Bakery Assistant",
                "Pazari i Ri, Tirana",
                75_000m,
                PayType.Monthly,
                JobType.Permanent,
                today.AddDays(3),
                null,
                ShiftType.Morning,
                null,
                null,
                2),
            new JobOpeningSeed(
                "Wedding Service Team",
                "Prepare the venue and provide table service throughout an afternoon and evening wedding celebration.",
                "Banquet Server",
                "Farkë, Tirana",
                6_000m,
                PayType.Daily,
                JobType.ShortTerm,
                today.AddDays(11),
                today.AddDays(11),
                ShiftType.CustomHours,
                new TimeOnly(14, 0),
                new TimeOnly(23, 0),
                10)
        ];
    }

    private sealed record JobOpeningSeed(
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
        IReadOnlyDictionary<string, JobOpeningTranslation>? Translations = null);
}
