namespace Workit.Core.Shared.Email;

public interface IEmailSender
{
    Task SendTemplateAsync(
        string toAddress,
        string templateId,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken cancellationToken = default);
}
