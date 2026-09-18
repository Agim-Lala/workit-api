using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Persistence;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Workers.Domain;

namespace Workit.Core.Workers;

public static class UpdateWorkerPreferences
{
    public sealed record Request(
        Guid WorkerUserId,
        IReadOnlyCollection<string> InterestedFields,
        IReadOnlyCollection<ShiftType> PreferredShiftTypes) : IRequest<Response>;

    public sealed record Response(
        IReadOnlyList<string> InterestedFields,
        IReadOnlyList<ShiftType> PreferredShiftTypes);

    public sealed class RequestValidator : AbstractValidator<Request>
    {
        public RequestValidator()
        {
            RuleFor(request => request.WorkerUserId)
                .NotEmpty();

            RuleFor(request => request.InterestedFields)
                .NotNull()
                .Must(fields => fields.Count <= WorkerProfile.MaxInterestedFieldsCount)
                .WithMessage(_ => $"Choose at most {WorkerProfile.MaxInterestedFieldsCount} interested fields.");

            RuleForEach(request => request.InterestedFields)
                .NotEmpty()
                .MaximumLength(WorkerProfile.MaxInterestedFieldLength);

            RuleFor(request => request.PreferredShiftTypes)
                .NotNull()
                .Must(shifts => shifts.Count <= WorkerProfile.MaxPreferredShiftTypesCount)
                .WithMessage("You already selected every shift type.");

            RuleForEach(request => request.PreferredShiftTypes)
                .IsInEnum();
        }
    }

    internal sealed class Handler(AppDbContext db, IDataWriter dataWriter)
        : IRequestHandler<Request, Response>
    {
        public async Task<Response> Handle(Request request, CancellationToken cancellationToken)
        {
            var profile = await db.Set<WorkerProfile>()
                .SingleOrDefaultAsync(
                    workerProfile => workerProfile.UserId == request.WorkerUserId,
                    cancellationToken)
                ?? throw new NotFoundException("error.workerProfileNotFound");

            profile.SetPreferences(request.InterestedFields, request.PreferredShiftTypes);
            await dataWriter.SaveAsync(cancellationToken);

            return new Response(profile.InterestedFields, profile.PreferredShiftTypes);
        }
    }
}
