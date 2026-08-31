using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataSeeders;
using Workit.Core.Shared.Time;
using Workit.Core.Users.Domain;
using Workit.Core.Workers.Domain;

namespace Workit.Api.Tests;

public sealed class WorkitDataSeederShould
{
    [Fact]
    public async Task SeedCompleteDemoDataWithoutDuplicates()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"workit-seeder-tests-{Guid.NewGuid()}")
            .Options;
        await using var db = new AppDbContext(options);
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash(Arg.Any<string>()).Returns("seed-password-hash");
        var clock = Substitute.For<IClock>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.Zero));
        var seeder = new WorkitDataSeeder(db, passwordHasher, clock);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var users = await db.Set<User>().ToListAsync();
        var businessProfiles = await db.Set<BusinessProfile>().ToListAsync();
        var workerProfiles = await db.Set<WorkerProfile>().ToListAsync();
        var jobOpenings = await db.Set<JobOpening>().ToListAsync();

        users.Count.ShouldBe(3);
        users.Select(user => user.Role)
            .ShouldBe(Enum.GetValues<UserRole>(), ignoreOrder: true);
        users.ShouldContain(user => user.Email == "worker@workit.al");
        users.ShouldContain(user => user.Email == "business@workit.al");
        users.ShouldContain(user => user.Email == "admin@workit.al");
        businessProfiles.ShouldHaveSingleItem();
        workerProfiles.ShouldHaveSingleItem();

        jobOpenings.Count.ShouldBe(8);
        jobOpenings.Select(jobOpening => jobOpening.Title).Distinct().Count().ShouldBe(8);
        jobOpenings.Select(jobOpening => jobOpening.JobType).Distinct()
            .ShouldBe(Enum.GetValues<JobType>(), ignoreOrder: true);
        jobOpenings.Select(jobOpening => jobOpening.PayType).Distinct()
            .ShouldBe(Enum.GetValues<PayType>(), ignoreOrder: true);
        jobOpenings.Select(jobOpening => jobOpening.ShiftType).Distinct()
            .ShouldBe(Enum.GetValues<ShiftType>(), ignoreOrder: true);

        jobOpenings.ShouldContain(jobOpening =>
            jobOpening.StartDate == jobOpening.EndDate
            && jobOpening.ShiftStartTime == new TimeOnly(16, 0)
            && jobOpening.ShiftEndTime == new TimeOnly(22, 0));
        jobOpenings.ShouldContain(jobOpening =>
            jobOpening.EndDate == jobOpening.StartDate.AddDays(1));
        jobOpenings.ShouldContain(jobOpening =>
            jobOpening.ShiftStartTime == new TimeOnly(22, 0)
            && jobOpening.ShiftEndTime == new TimeOnly(6, 0));
        jobOpenings
            .Where(jobOpening => jobOpening.JobType == JobType.Permanent)
            .ShouldAllBe(jobOpening => !jobOpening.EndDate.HasValue);
        jobOpenings
            .Where(jobOpening => jobOpening.JobType != JobType.Permanent)
            .ShouldAllBe(jobOpening => jobOpening.EndDate.HasValue);
    }
}
