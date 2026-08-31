namespace Workit.Core.JobOpenings.Domain;

public sealed class JobOpening
{
    public const int MaxTitleLength = 160;
    public const int MaxDescriptionLength = 4000;
    public const int MaxRoleLength = 120;
    public const int MaxLocationLength = 500;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid BusinessProfileId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public decimal PayAmount { get; private set; }
    public PayType PayType { get; private set; }
    public JobType JobType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public ShiftType ShiftType { get; private set; }
    public TimeOnly? ShiftStartTime { get; private set; }
    public TimeOnly? ShiftEndTime { get; private set; }
    public int RequiredWorkersCount { get; private set; }
    public JobOpeningStatus Status { get; private set; } = JobOpeningStatus.Open;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private JobOpening()
    {
    }

    public JobOpening(
        Guid businessProfileId,
        string title,
        string description,
        string role,
        string location,
        decimal payAmount,
        PayType payType,
        JobType jobType,
        DateOnly startDate,
        DateOnly? endDate,
        ShiftType shiftType,
        TimeOnly? shiftStartTime,
        TimeOnly? shiftEndTime,
        int requiredWorkersCount,
        DateTimeOffset createdAt)
    {
        ValidateSchedule(jobType, startDate, endDate, shiftType, shiftStartTime, shiftEndTime);

        BusinessProfileId = businessProfileId;
        Title = title.Trim();
        Description = description.Trim();
        Role = role.Trim();
        Location = location.Trim();
        PayAmount = payAmount;
        PayType = payType;
        JobType = jobType;
        StartDate = startDate;
        EndDate = endDate;
        ShiftType = shiftType;
        ShiftStartTime = shiftStartTime;
        ShiftEndTime = shiftEndTime;
        RequiredWorkersCount = requiredWorkersCount;
        CreatedAt = createdAt;
    }

    private static void ValidateSchedule(
        JobType jobType,
        DateOnly startDate,
        DateOnly? endDate,
        ShiftType shiftType,
        TimeOnly? shiftStartTime,
        TimeOnly? shiftEndTime)
    {
        if (jobType == JobType.Permanent && endDate.HasValue)
        {
            throw new ArgumentException("Permanent jobs cannot have an end date.", nameof(endDate));
        }

        if (jobType != JobType.Permanent && !endDate.HasValue)
        {
            throw new ArgumentException("Project and short-term jobs require an end date.", nameof(endDate));
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("The end date cannot be before the start date.", nameof(endDate));
        }

        var hasBothCustomTimes = shiftStartTime.HasValue && shiftEndTime.HasValue;
        if (shiftType == ShiftType.CustomHours
            && (!hasBothCustomTimes || shiftStartTime == shiftEndTime))
        {
            throw new ArgumentException(
                "Custom-hours shifts require different start and end times.",
                nameof(shiftStartTime));
        }

        if (shiftType != ShiftType.CustomHours
            && (shiftStartTime.HasValue || shiftEndTime.HasValue))
        {
            throw new ArgumentException(
                "Morning and evening shifts cannot include custom hours.",
                nameof(shiftStartTime));
        }
    }
}
