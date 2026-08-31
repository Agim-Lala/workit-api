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
    public JobScheduleType ScheduleType { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset? EndsAt { get; private set; }
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
        JobScheduleType scheduleType,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        int requiredWorkersCount,
        DateTimeOffset createdAt)
    {
        BusinessProfileId = businessProfileId;
        Title = title.Trim();
        Description = description.Trim();
        Role = role.Trim();
        Location = location.Trim();
        PayAmount = payAmount;
        PayType = payType;
        ScheduleType = scheduleType;
        StartsAt = startsAt;
        EndsAt = endsAt;
        RequiredWorkersCount = requiredWorkersCount;
        CreatedAt = createdAt;
    }
}
