namespace Workit.Core.Shared.Entities;

public abstract record Entity<T>(T Id)
{
    public virtual bool Equals(Entity<T>? other)
    {
        return other is not null && EqualityComparer<T>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode()
    {
        return Id?.GetHashCode() ?? 0;
    }
}

public abstract record EntityWithGuid(DateTimeOffset CreatedAt) : Entity<Guid>(Guid.NewGuid());

public abstract record EntityWithTracking<T>(
    T Id,
    DateTimeOffset CreatedAt)
    : Entity<T>(Id)
{
    public DateTimeOffset LastUpdatedAt { get; init; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; init; }
    public Guid? LastUpdatedBy { get; init; }
}
