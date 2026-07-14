namespace Workit.Core.Shared.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
