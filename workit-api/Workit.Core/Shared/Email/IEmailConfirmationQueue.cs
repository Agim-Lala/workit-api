namespace Workit.Core.Shared.Email;

/// <summary>
/// Queues a confirmation-email send so the HTTP request that triggers it (registration, resend)
/// returns without waiting on Resend's third-party latency. In-memory only — an item not yet
/// drained when the process restarts is lost; fine for a "nice to have" transactional email,
/// not for anything that must survive a crash.
/// </summary>
public interface IEmailConfirmationQueue
{
    void Enqueue(string toEmail, string plainToken);

    IAsyncEnumerable<(string ToEmail, string PlainToken)> ReadAllAsync(CancellationToken cancellationToken);
}
