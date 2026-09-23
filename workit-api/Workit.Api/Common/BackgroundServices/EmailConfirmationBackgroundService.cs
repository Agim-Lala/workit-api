using Workit.Core.Shared.Email;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Users.Shared;

namespace Workit.Api.Common.BackgroundServices;

/// <summary>
/// Drains <see cref="IEmailConfirmationQueue"/> and does the actual Resend call, so registration
/// and resend-confirmation requests return without waiting on that third-party round trip. A
/// fresh DI scope per item avoids holding scoped services (e.g. <see cref="IEmailSender"/>) for
/// the process lifetime.
/// </summary>
public sealed class EmailConfirmationBackgroundService(
    IEmailConfirmationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailConfirmationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var (toEmail, plainToken) in queue.ReadAllAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
            var settings = scope.ServiceProvider.GetRequiredService<WorkitSettings>();

            await EmailConfirmationSender.SendAsync(emailSender, settings, logger, toEmail, plainToken, stoppingToken);
        }
    }
}
