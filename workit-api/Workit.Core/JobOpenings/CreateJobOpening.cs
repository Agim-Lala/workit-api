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
        Guid BusinessUserId,
        string Title,
        string Description,
        string Role,
        string Location,
        decimal PayAmount,
        PayType PayType,
        JobType JobType,
        DateOnly StartDate,
        DateOnly? EndDate,
        ShiftType ShiftType,
        TimeOnly? ShiftStartTime,
        TimeOnly? ShiftEndTime,
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
        JobType JobType,
        DateOnly StartDate,
        DateOnly? EndDate,
        ShiftType ShiftType,
        TimeOnly? ShiftStartTime,
        TimeOnly? ShiftEndTime,
        int RequiredWorkersCount,
        JobOpeningStatus Status,
        DateTimeOffset CreatedAt);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.BusinessUserId)
                .NotEmpty();

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

            RuleFor(request => request.JobType)
                .IsInEnum();

            RuleFor(request => request.StartDate)
                .NotEmpty();

            RuleFor(request => request.EndDate)
                .Null()
                .When(request => request.JobType == JobType.Permanent)
                .WithMessage("Permanent jobs cannot have an end date.");

            RuleFor(request => request.EndDate)
                .NotNull()
                .When(request => request.JobType is JobType.Project or JobType.ShortTerm)
                .WithMessage("Project and short-term jobs require an end date.");

            RuleFor(request => request.EndDate)
                .GreaterThanOrEqualTo(request => request.StartDate)
                .When(request => request.EndDate.HasValue);

            RuleFor(request => request.ShiftType)
                .IsInEnum();

            RuleFor(request => request.ShiftStartTime)
                .NotNull()
                .When(request => request.ShiftType == ShiftType.CustomHours)
                .WithMessage("Custom-hours shifts require a start time.");

            RuleFor(request => request.ShiftEndTime)
                .NotNull()
                .When(request => request.ShiftType == ShiftType.CustomHours)
                .WithMessage("Custom-hours shifts require an end time.");

            RuleFor(request => request)
                .Must(request => request.ShiftStartTime != request.ShiftEndTime)
                .When(request => request.ShiftType == ShiftType.CustomHours
                    && request.ShiftStartTime.HasValue
                    && request.ShiftEndTime.HasValue)
                .WithName(nameof(Request.ShiftEndTime))
                .WithMessage("Custom-hours shift start and end times must be different.");

            RuleFor(request => request.ShiftStartTime)
                .Null()
                .When(request => request.ShiftType != ShiftType.CustomHours)
                .WithMessage("Morning and evening shifts cannot include custom hours.");

            RuleFor(request => request.ShiftEndTime)
                .Null()
                .When(request => request.ShiftType != ShiftType.CustomHours)
                .WithMessage("Morning and evening shifts cannot include custom hours.");

            RuleFor(request => request.RequiredWorkersCount)
                .GreaterThan(0)
                .LessThanOrEqualTo(1000);
        }
    }

    internal sealed class Handler(
        AppDbContext db,
        IDataWriter dataWriter,
        IClock clock)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var businessProfileId = await db.Set<BusinessProfile>()
                .Where(businessProfile => businessProfile.UserId == request.BusinessUserId)
                .Select(businessProfile => businessProfile.Id)
                .SingleOrDefaultAsync(cancellationToken);

            if (businessProfileId == Guid.Empty)
            {
                throw new NotFoundException("Business profile not found.");
            }

            var now = clock.UtcNow;
            var jobOpening = new JobOpening(
                businessProfileId,
                request.Title,
                request.Description,
                request.Role,
                request.Location,
                request.PayAmount,
                request.PayType,
                request.JobType,
                request.StartDate,
                request.EndDate,
                request.ShiftType,
                request.ShiftStartTime,
                request.ShiftEndTime,
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
                jobOpening.JobType,
                jobOpening.StartDate,
                jobOpening.EndDate,
                jobOpening.ShiftType,
                jobOpening.ShiftStartTime,
                jobOpening.ShiftEndTime,
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
