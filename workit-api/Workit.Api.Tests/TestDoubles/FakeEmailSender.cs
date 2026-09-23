using Workit.Core.Shared.Email;

namespace Workit.Api.Tests.TestDoubles;

/// <summary>
/// Test double for <see cref="IEmailSender"/> so API tests never call the real Resend API
/// and can inspect what would have been sent (e.g. to pull a confirmation link's token).
/// </summary>
public sealed class FakeEmailSender : IEmailSender
{
    public List<(string ToAddress, string TemplateId, IReadOnlyDictionary<string, string> Variables)> SentEmails { get; } = [];

    public Task SendTemplateAsync(
        string toAddress,
        string templateId,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default)
    {
        SentEmails.Add((toAddress, templateId, variables));
        return Task.CompletedTask;
    }
}
