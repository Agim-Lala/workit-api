using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Workit.Core.Shared.Email;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Shared.Tokens;
using Workit.Core.Users.Domain;
using Workit.Core.Users.Shared;

namespace Workit.Core.Users;

/// <summary>
/// Resends the confirmation link. Always responds the same way whether or not the address is
/// registered or already confirmed, so the endpoint can't be used to enumerate accounts.
/// </summary>
public static class ResendEmailConfirmation
{
    public sealed record Request(string Email) : IRequest<Response>;

    public sealed record Response;

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(User.MaxEmailLength);
        }
    }

    internal sealed class Handler(
        AppDbContext db,
        IDataWriter dataWriter,
        ITokenService tokenService,
        IEmailSender emailSender,
        WorkitSettings settings,
        IClock clock,
        ILogger<Handler> logger)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var normalizedEmail = User.NormalizeEmail(request.Email);
            var user = await db.Set<User>()
                .SingleOrDefaultAsync(user => user.Email == normalizedEmail, cancellationToken);

            if (user is not null && !user.EmailConfirmed)
            {
                var now = clock.UtcNow;
                var (plainToken, tokenHash) = tokenService.GenerateToken();
                user.SetEmailConfirmationToken(tokenHash, now.AddHours(settings.Email.ConfirmationTokenExpirationInHours));
                await dataWriter.Update(user).SaveAsync(cancellationToken);

                await EmailConfirmationSender.SendAsync(emailSender, settings, logger, user.Email, plainToken, cancellationToken);
            }

            return new Response();
        }
    }
}
