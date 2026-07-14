namespace Workit.Core.Shared.Jobs;

public interface IBackgroundJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
