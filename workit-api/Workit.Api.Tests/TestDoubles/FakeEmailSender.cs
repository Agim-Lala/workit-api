using Workit.Core.Shared.Email;

namespace Workit.Api.Tests.TestDoubles;

/// <summary>
/// Test double for <see cref="IEmailSender"/> so API tests never attempt a real SMTP connection
/// and can inspect what would have been sent (e.g. to pull a confirmation link's token).
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    public List<(string ToAddress, string Subject, string HtmlBody)> SentEmails { get; } = [];

    public Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        SentEmails.Add((toAddress, subject, htmlBody));
        return Task.CompletedTask;
    }
}
