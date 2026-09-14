using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.JobOpenings;
using Workit.Core.JobOpenings.Domain;

namespace Workit.Api.JobOpenings;

public sealed class CreateJobOpeningEndpoint : IRouteMapper
{
    public sealed record Body(
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
        string? ContentLanguage = null,
        IReadOnlyDictionary<string, JobOpeningTranslation>? Translations = null);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/job-openings", CreateAsync)
            .RequireAuthorization(AuthorizationPolicies.BusinessOnly)
            .Produces<CreateJobOpening.Response>(StatusCodes.Status201Created)
            .WithName(nameof(CreateJobOpening))
            .WithTags("Job Openings");
    }

    private static async Task<IResult> CreateAsync(
        Body request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var businessUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(
            new CreateJobOpening.Request(
                businessUserId,
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
                request.ContentLanguage,
                request.Translations),
            cancellationToken);
        return Results.Created($"/job-openings/{response.Id}", response);
    }
}
