using Microsoft.EntityFrameworkCore;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Time;
using Workit.Core.Users.Domain;

namespace Workit.Core.Shared.Persistence.DataSeeders;

public sealed class WorkitDataSeeder(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IClock clock)
    : IDataSeeder
{
    private const string SeedPassword = "Test@1234";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedUserAsync("user@workit.al", UserRole.Worker, cancellationToken);
        await SeedUserAsync("admin@workit.al", UserRole.Admin, cancellationToken);
        await SeedBusinessWithJobOpeningAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<User> SeedUserAsync(
        string email,
        UserRole role,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = User.NormalizeEmail(email);
        var user = await db.Set<User>()
            .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (user is not null)
        {
            return user;
        }

        user = new User(
            normalizedEmail,
            passwordHasher.Hash(SeedPassword),
            clock.UtcNow,
            role);

        db.Set<User>().Add(user);
        return user;
    }

    private async Task SeedBusinessWithJobOpeningAsync(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var businessUser = await SeedUserAsync("business@workit.al", UserRole.Business, cancellationToken);
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
                now,
                "+355 69 000 0000");

            db.Set<BusinessProfile>().Add(businessProfile);
        }

        var jobOpeningExists = await db.Set<JobOpening>()
            .AnyAsync(jobOpening =>
                jobOpening.BusinessProfileId == businessProfile.Id
                && jobOpening.Title == "Weekend Event Staff",
                cancellationToken);

        if (jobOpeningExists)
        {
            return;
        }

        db.Set<JobOpening>().Add(new JobOpening(
            businessProfile.Id,
            "Weekend Event Staff",
            "Support event setup, guest check-in, and floor operations for weekend shifts.",
            "Event Staff",
            "Tirana",
            6m,
            PayType.Hourly,
            JobScheduleType.RecurringWeekly,
            now.Date.AddDays(7).AddHours(17),
            now.Date.AddDays(37).AddHours(23),
            4,
            now));
    }
}
