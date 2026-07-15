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
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

public static class RegisterWorker
{
    public sealed record Request(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string? Phone = null) : IRequest<Response>;

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

            RuleFor(request => request.FirstName)
                .NotEmpty()
                .MaximumLength(WorkerProfile.MaxNameLength);

            RuleFor(request => request.LastName)
                .NotEmpty()
                .MaximumLength(WorkerProfile.MaxNameLength);

            RuleFor(request => request.Phone)
                .MaximumLength(WorkerProfile.MaxPhoneLength)
                .When(request => !string.IsNullOrWhiteSpace(request.Phone));
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
            var now = clock.UtcNow;
            var user = new User(
                request.Email,
                passwordHasher.Hash(request.Password),
                now,
                UserRole.Worker);
            var workerProfile = new WorkerProfile(
                user.Id,
                request.FirstName,
                request.LastName,
                now,
                request.Phone);

            try
            {
                await dataWriter
                    .Add(user)
                    .Add(workerProfile)
                    .SaveAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                throw new DomainException("Email already registered.");
            }

            return CreateResponse(user, now, accessTokenCreator, settings);
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
