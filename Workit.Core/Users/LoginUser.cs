using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Time;
using Workit.Core.Shared.Tokens;
using Workit.Core.Users.Domain;
using Workit.Core.Users.Shared;

namespace Workit.Core.Users;

public static class LoginUser
{
    public sealed record Request(string Email, string Password) : IRequest<Response>;

    public sealed record Response(UserDto User, string AccessToken, DateTimeOffset ExpiresAt);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(User.MaxEmailLength);

            RuleFor(request => request.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);
        }
    }

    internal sealed class Handler(
        ReadAppDbContext db,
        IPasswordHasher passwordHasher,
        IClock clock,
        IAccessTokenCreator accessTokenCreator,
        WorkitSettings settings)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var email = User.NormalizeEmail(request.Email);
            var user = await db.Set<User>()
                .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

            if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new DomainException("Invalid email or password.");
            }

            var expiresAt = clock.UtcNow.AddMinutes(settings.Token.ExpirationInMinutes);
            return new Response(
                new UserDto(user.Id, user.Email, user.Role),
                accessTokenCreator.Create(user, expiresAt),
                expiresAt);
        }
    }
}
