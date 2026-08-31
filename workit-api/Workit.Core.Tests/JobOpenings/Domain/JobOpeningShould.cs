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

    [Fact]
    public void RepresentAOneDayCustomShift()
    {
        var workDate = new DateOnly(2026, 8, 8);

        var jobOpening = CreateJobOpening(
            JobType.ShortTerm,
            workDate,
            workDate,
            ShiftType.CustomHours,
            new TimeOnly(16, 0),
            new TimeOnly(22, 0));

        jobOpening.StartDate.ShouldBe(workDate);
        jobOpening.EndDate.ShouldBe(workDate);
        jobOpening.ShiftStartTime.ShouldBe(new TimeOnly(16, 0));
        jobOpening.ShiftEndTime.ShouldBe(new TimeOnly(22, 0));
    }

    [Fact]
    public void AllowPermanentJobsWithANamedShiftAndNoEndDate()
    {
        var jobOpening = CreateJobOpening(
            JobType.Permanent,
            new DateOnly(2026, 9, 1),
            null,
            ShiftType.Morning,
            null,
            null);

        jobOpening.JobType.ShouldBe(JobType.Permanent);
        jobOpening.EndDate.ShouldBeNull();
        jobOpening.ShiftType.ShouldBe(ShiftType.Morning);
    }

    [Fact]
    public void AllowTwoDayShortTermJobs()
    {
        var startDate = new DateOnly(2026, 9, 1);

        var jobOpening = CreateJobOpening(
            JobType.ShortTerm,
            startDate,
            startDate.AddDays(1),
            ShiftType.Evening,
            null,
            null);

        jobOpening.StartDate.ShouldBe(startDate);
        jobOpening.EndDate.ShouldBe(startDate.AddDays(1));
    }

    [Fact]
    public void AllowCustomShiftThatEndsAfterMidnight()
    {
        var workDate = new DateOnly(2026, 9, 1);

        var jobOpening = CreateJobOpening(
            JobType.ShortTerm,
            workDate,
            workDate,
            ShiftType.CustomHours,
            new TimeOnly(22, 0),
            new TimeOnly(6, 0));

        jobOpening.ShiftStartTime.ShouldBe(new TimeOnly(22, 0));
        jobOpening.ShiftEndTime.ShouldBe(new TimeOnly(6, 0));
    }

    [Fact]
    public void RejectProjectWithoutEndDate()
    {
        Should.Throw<ArgumentException>(() => CreateJobOpening(
                JobType.Project,
                new DateOnly(2026, 9, 1),
                null,
                ShiftType.Evening,
                null,
                null))
            .Message.ShouldContain("require an end date");
    }

    [Fact]
    public void RejectPermanentJobWithEndDate()
    {
        Should.Throw<ArgumentException>(() => CreateJobOpening(
                JobType.Permanent,
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 30),
                ShiftType.Morning,
                null,
                null))
            .Message.ShouldContain("cannot have an end date");
    }

    [Fact]
    public void RejectEndDateBeforeStartDate()
    {
        Should.Throw<ArgumentException>(() => CreateJobOpening(
                JobType.ShortTerm,
                new DateOnly(2026, 9, 2),
                new DateOnly(2026, 9, 1),
                ShiftType.Evening,
                null,
                null))
            .Message.ShouldContain("before the start date");
    }

    [Fact]
    public void RejectCustomShiftWithoutBothTimes()
    {
        Should.Throw<ArgumentException>(() => CreateJobOpening(
                JobType.ShortTerm,
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 1),
                ShiftType.CustomHours,
                new TimeOnly(16, 0),
                null))
            .Message.ShouldContain("require different start and end times");
    }

    private static JobOpening CreateJobOpening()
    {
        return CreateJobOpening(
            JobType.ShortTerm,
            new DateOnly(2026, 8, 1),
            new DateOnly(2026, 8, 31),
            ShiftType.CustomHours,
            new TimeOnly(18, 0),
            new TimeOnly(23, 0));
    }

    private static JobOpening CreateJobOpening(
        JobType jobType,
        DateOnly startDate,
        DateOnly? endDate,
        ShiftType shiftType,
        TimeOnly? shiftStartTime,
        TimeOnly? shiftEndTime)
    {
        return new JobOpening(
            Guid.NewGuid(),
            "  Weekend bartender  ",
            "  Friday and Saturday evening shifts.  ",
            "  Bartender  ",
            "  Tirana  ",
            8.5m,
            PayType.Hourly,
            jobType,
            startDate,
            endDate,
            shiftType,
            shiftStartTime,
            shiftEndTime,
            2,
            DateTimeOffset.UtcNow);
    }
}
