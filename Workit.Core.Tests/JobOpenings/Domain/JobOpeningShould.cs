using Shouldly;
using Workit.Core.JobOpenings.Domain;

namespace Workit.Core.Tests.JobOpenings.Domain;

public sealed class JobOpeningShould
{
    [Fact]
    public void TrimTextValuesWhenCreated()
    {
        var jobOpening = CreateJobOpening();

        jobOpening.Title.ShouldBe("Weekend bartender");
        jobOpening.Description.ShouldBe("Friday and Saturday evening shifts.");
        jobOpening.Role.ShouldBe("Bartender");
        jobOpening.Location.ShouldBe("Tirana");
    }

    [Fact]
    public void StartAsOpenWhenCreated()
    {
        var jobOpening = CreateJobOpening();

        jobOpening.Status.ShouldBe(JobOpeningStatus.Open);
    }

    private static JobOpening CreateJobOpening()
    {
        var startsAt = new DateTimeOffset(2026, 8, 1, 18, 0, 0, TimeSpan.Zero);
        var endsAt = new DateTimeOffset(2026, 8, 31, 23, 0, 0, TimeSpan.Zero);

        return new JobOpening(
            Guid.NewGuid(),
            "  Weekend bartender  ",
            "  Friday and Saturday evening shifts.  ",
            "  Bartender  ",
            "  Tirana  ",
            8.5m,
            PayType.Hourly,
            JobScheduleType.RecurringWeekly,
            startsAt,
            endsAt,
            2,
            DateTimeOffset.UtcNow);
    }
}
