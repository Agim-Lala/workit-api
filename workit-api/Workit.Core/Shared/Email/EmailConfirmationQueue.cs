using System.Threading.Channels;

namespace Workit.Core.Shared.Email;

internal sealed class EmailConfirmationQueue : IEmailConfirmationQueue
{
    private readonly Channel<(string ToEmail, string PlainToken)> channel =
        Channel.CreateUnbounded<(string ToEmail, string PlainToken)>();

    public void Enqueue(string toEmail, string plainToken) =>
        channel.Writer.TryWrite((toEmail, plainToken));

    public IAsyncEnumerable<(string ToEmail, string PlainToken)> ReadAllAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAllAsync(cancellationToken);
}
