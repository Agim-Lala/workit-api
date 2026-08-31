namespace Workit.Core.Workers.Domain;

public sealed class WorkerProfile
{
    public const int MaxNameLength = 100;
    public const int MaxPhoneLength = 50;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private WorkerProfile()
    {
    }

    public WorkerProfile(
        Guid userId,
        string firstName,
        string lastName,
        DateTimeOffset createdAt,
        string? phone = null)
    {
        UserId = userId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        CreatedAt = createdAt;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
