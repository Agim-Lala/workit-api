using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Shared.Tokens;
using Workit.Core.Users.Domain;

namespace Workit.Core.Users;

public static class ConfirmEmail
{
    public sealed record Request(string Token) : IRequest<Response>;

    public sealed record Response;

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.Token).NotEmpty();
        }
    }

    internal sealed class Handler(AppDbContext db, IDataWriter dataWriter, ITokenService tokenService, IClock clock)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var tokenHash = tokenService.HashToken(request.Token);
            var user = await db.Set<User>()
                .SingleOrDefaultAsync(user => user.EmailConfirmationTokenHash == tokenHash, cancellationToken)
                ?? throw new DomainException("error.confirmationTokenInvalid");

            if (!user.HasValidEmailConfirmationToken(tokenHash, clock.UtcNow))
            {
                throw new DomainException("error.confirmationTokenExpired");
            }

            user.ConfirmEmail();
            await dataWriter.Update(user).SaveAsync(cancellationToken);

            return new Response();
        }
    }
}
