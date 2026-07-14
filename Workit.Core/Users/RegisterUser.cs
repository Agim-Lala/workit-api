using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Shared.Tokens;
using Workit.Core.Users.Domain;
using Workit.Core.Users.Shared;

namespace Workit.Core.Users;

public static class RegisterUser
{
    public sealed record Request(string Email, string Password) : IRequest<Response>;

    public sealed record Response(UserDto User, string AccessToken, DateTimeOffset ExpiresAt);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        private readonly ReadAppDbContext db;

        public RequestValidator(ReadAppDbContext db)
        {
            this.db = db;

            RuleFor(request => request.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(User.MaxEmailLength)
                .MustAsync(BeAvailableEmail)
                .WithMessage("Email already registered.");

            RuleFor(request => request.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);
        }

        private async Task<bool> BeAvailableEmail(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = User.NormalizeEmail(email);
            return !await db.Set<User>()
                .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        }
    }

    internal sealed class Handler(
        IDataWriter dataWriter,
        IPasswordHasher passwordHasher,
        IClock clock,
        IAccessTokenCreator accessTokenCreator,
        WorkitSettings settings)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var user = new User(
                request.Email,
                passwordHasher.Hash(request.Password),
                clock.UtcNow);

            try
            {
                await dataWriter
                    .Add(user)
                    .SaveAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                throw new DomainException("Email already registered.");
            }

            return CreateResponse(user, clock.UtcNow, accessTokenCreator, settings);
        }

        private static Response CreateResponse(
            User user,
            DateTimeOffset now,
            IAccessTokenCreator accessTokenCreator,
            WorkitSettings settings)
        {
            var expiresAt = now.AddMinutes(settings.Token.ExpirationInMinutes);
            return new Response(
                new UserDto(user.Id, user.Email, user.Role),
                accessTokenCreator.Create(user, expiresAt),
                expiresAt);
        }

        private static bool IsUniqueViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
        }
    }
}
