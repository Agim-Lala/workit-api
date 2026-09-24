using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Workit.Core.Businesses.Domain;
using Workit.Core.Shared.Email;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Location;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;
using Workit.Core.Shared.Tokens;
using Workit.Core.Users.Domain;
using Workit.Core.Users.Shared;

namespace Workit.Core.Businesses;

public static partial class RegisterBusiness
{
    private const string NiptDatabaseConstraintName = "ix_business_profiles_nipt";

    public sealed record Request(
        string Email,
        string Password,
        string BusinessName,
        string FullAddress,
        decimal? Latitude,
        decimal? Longitude,
        string Nipt,
        string? Phone = null) : IRequest<Response>;

    public sealed record Response(UserDto User, string AccessToken, DateTimeOffset ExpiresAt);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        private readonly ReadAppDbContext db;

        public RequestValidator(ReadAppDbContext db, ILocalizer localizer)
        {
            this.db = db;

            RuleFor(request => request.Email)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(User.MaxEmailLength)
                .MustAsync(BeAvailableEmail)
                .WithMessage(_ => localizer.Translate("validation.user.emailAlreadyRegistered"));

            RuleFor(request => request.Password)
                .NotEmpty()
                .MinimumLength(8)
                .MaximumLength(128);

            RuleFor(request => request.BusinessName)
                .NotEmpty()
                .MaximumLength(BusinessProfile.MaxBusinessNameLength);

            RuleFor(request => request.FullAddress)
                .NotEmpty()
                .MaximumLength(BusinessProfile.MaxFullAddressLength);

            RuleFor(request => request.Latitude)
                .InclusiveBetween(-90m, 90m);

            RuleFor(request => request.Longitude)
                .InclusiveBetween(-180m, 180m);

            RuleFor(request => request.Nipt)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(BeAValidNiptFormat)
                .WithMessage(_ => localizer.Translate("validation.business.niptInvalidFormat"))
                .MustAsync(BeAvailableNipt)
                .WithMessage(_ => localizer.Translate("validation.business.niptAlreadyRegistered"));

            RuleFor(request => request.Phone)
                .MaximumLength(BusinessProfile.MaxPhoneLength)
                .When(request => !string.IsNullOrWhiteSpace(request.Phone));
        }

        private async Task<bool> BeAvailableEmail(string email, CancellationToken cancellationToken)
        {
            var normalizedEmail = User.NormalizeEmail(email);
            return !await db.Set<User>()
                .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);
        }

        private static bool BeAValidNiptFormat(string nipt)
        {
            return NiptFormat().IsMatch(BusinessProfile.NormalizeNipt(nipt));
        }

        private async Task<bool> BeAvailableNipt(string nipt, CancellationToken cancellationToken)
        {
            var normalizedNipt = BusinessProfile.NormalizeNipt(nipt);
            return !await db.Set<BusinessProfile>()
                .AnyAsync(businessProfile => businessProfile.Nipt == normalizedNipt, cancellationToken);
        }
    }

    [GeneratedRegex("^[A-Z]\\d{8}[A-Z]$")]
    private static partial Regex NiptFormat();

    internal sealed class Handler(
        IDataWriter dataWriter,
        IPasswordHasher passwordHasher,
        IClock clock,
        IAccessTokenCreator accessTokenCreator,
        ITokenService tokenService,
        IEmailConfirmationQueue emailConfirmationQueue,
        WorkitSettings settings,
        ILocalizer localizer,
        ICityLookupService cityLookupService)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var (latitude, longitude) = await ResolveCoordinatesAsync(request, cancellationToken);
            var now = clock.UtcNow;
            var user = new User(
                request.Email,
                passwordHasher.Hash(request.Password),
                now,
                UserRole.Business);
            var businessProfile = new BusinessProfile(
                user.Id,
                request.BusinessName,
                request.FullAddress,
                latitude,
                longitude,
                request.Nipt,
                now,
                request.Phone);

            var (plainToken, tokenHash) = tokenService.GenerateToken();
            user.SetEmailConfirmationToken(tokenHash, now.AddHours(settings.Email.ConfirmationTokenExpirationInHours));

            try
            {
                await dataWriter
                    .Add(user)
                    .Add(businessProfile)
                    .SaveAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception, NiptDatabaseConstraintName))
            {
                throw new DomainException("error.niptAlreadyRegistered");
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                throw new DomainException("error.emailAlreadyRegistered");
            }

            emailConfirmationQueue.Enqueue(user.Email, plainToken);

            return CreateResponse(user, now, accessTokenCreator, settings, localizer);
        }

        // Clients with a map pin send coordinates; otherwise the typed address is geocoded.
        // Unlike workers, a business must always have coordinates, so a miss is a hard reject.
        private async Task<(decimal Latitude, decimal Longitude)> ResolveCoordinatesAsync(
            Request request,
            CancellationToken cancellationToken)
        {
            if (request is { Latitude: { } latitude, Longitude: { } longitude })
            {
                return (latitude, longitude);
            }

            var match = await cityLookupService.FindAsync(request.FullAddress, cancellationToken)
                ?? throw new DomainException("error.businessAddressNotFound");
            return ((decimal)match.Latitude, (decimal)match.Longitude);
        }

        private static Response CreateResponse(
            User user,
            DateTimeOffset now,
            IAccessTokenCreator accessTokenCreator,
            WorkitSettings settings,
            ILocalizer localizer)
        {
            var expiresAt = now.AddMinutes(settings.Token.ExpirationInMinutes);
            var emailConfirmationStatus = user.GetEmailConfirmationStatus(now);
            return new Response(
                new UserDto(
                    user.Id,
                    user.Email,
                    user.Role,
                    localizer.Enum(user.Role),
                    emailConfirmationStatus,
                    localizer.Enum(emailConfirmationStatus)),
                accessTokenCreator.Create(user, expiresAt),
                expiresAt);
        }

        private static bool IsUniqueViolation(DbUpdateException exception, string? constraintName = null)
        {
            return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException
                && (constraintName is null || postgresException.ConstraintName == constraintName);
        }
    }
}
