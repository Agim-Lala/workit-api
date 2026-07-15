using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Workit.Core.Businesses.Domain;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Time;

namespace Workit.Core.JobOpenings;

public static class CreateJobOpening
{
    public sealed record Request(
        Guid BusinessProfileId,
        string Title,
        string Description,
        string Role,
        string Location,
        decimal PayAmount,
        PayType PayType,
        JobScheduleType ScheduleType,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int RequiredWorkersCount) : IRequest<Response>;

    public sealed record Response(
        Guid Id,
        Guid BusinessProfileId,
        string Title,
        string Description,
        string Role,
        string Location,
        decimal PayAmount,
        PayType PayType,
        JobScheduleType ScheduleType,
        DateTimeOffset StartsAt,
        DateTimeOffset? EndsAt,
        int RequiredWorkersCount,
        JobOpeningStatus Status,
        DateTimeOffset CreatedAt);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        private readonly ReadAppDbContext db;

        public RequestValidator(ReadAppDbContext db)
        {
            this.db = db;

            RuleFor(request => request.BusinessProfileId)
                .NotEmpty()
                .MustAsync(ExistBusinessProfile)
                .WithMessage("Business profile not found.");

            RuleFor(request => request.Title)
                .NotEmpty()
                .MaximumLength(JobOpening.MaxTitleLength);

            RuleFor(request => request.Description)
                .NotEmpty()
                .MaximumLength(JobOpening.MaxDescriptionLength);

            RuleFor(request => request.Role)
                .NotEmpty()
                .MaximumLength(JobOpening.MaxRoleLength);

            RuleFor(request => request.Location)
                .NotEmpty()
                .MaximumLength(JobOpening.MaxLocationLength);

            RuleFor(request => request.PayAmount)
                .GreaterThan(0);

            RuleFor(request => request.PayType)
                .IsInEnum();

            RuleFor(request => request.ScheduleType)
                .IsInEnum();

            RuleFor(request => request.StartsAt)
                .NotEmpty();

            RuleFor(request => request.EndsAt)
                .GreaterThan(request => request.StartsAt)
                .When(request => request.EndsAt.HasValue);

            RuleFor(request => request.RequiredWorkersCount)
                .GreaterThan(0)
                .LessThanOrEqualTo(1000);
        }

        private async Task<bool> ExistBusinessProfile(Guid businessProfileId, CancellationToken cancellationToken)
        {
            return await db.Set<BusinessProfile>()
                .AnyAsync(businessProfile => businessProfile.Id == businessProfileId, cancellationToken);
        }
    }

    internal sealed class Handler(
        IDataWriter dataWriter,
        IClock clock)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var now = clock.UtcNow;
            var jobOpening = new JobOpening(
                request.BusinessProfileId,
                request.Title,
                request.Description,
                request.Role,
                request.Location,
                request.PayAmount,
                request.PayType,
                request.ScheduleType,
                request.StartsAt,
                request.EndsAt,
                request.RequiredWorkersCount,
                now);

            try
            {
                await dataWriter
                    .Add(jobOpening)
                    .SaveAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsForeignKeyViolation(exception))
            {
                throw new NotFoundException("Business profile not found.");
            }

            return new Response(
                jobOpening.Id,
                jobOpening.BusinessProfileId,
                jobOpening.Title,
                jobOpening.Description,
                jobOpening.Role,
                jobOpening.Location,
                jobOpening.PayAmount,
                jobOpening.PayType,
                jobOpening.ScheduleType,
                jobOpening.StartsAt,
                jobOpening.EndsAt,
                jobOpening.RequiredWorkersCount,
                jobOpening.Status,
                jobOpening.CreatedAt);
        }

        private static bool IsForeignKeyViolation(DbUpdateException exception)
        {
            return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation };
        }
    }
}
