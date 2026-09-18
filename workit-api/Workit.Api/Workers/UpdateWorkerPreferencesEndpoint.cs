using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.JobOpenings.Domain;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

public sealed class UpdateWorkerPreferencesEndpoint : IRouteMapper
{
    public sealed record Body(
        IReadOnlyCollection<string> InterestedFields,
        IReadOnlyCollection<ShiftType> PreferredShiftTypes);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/worker-profile/preferences", UpdateAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            .Produces<UpdateWorkerPreferences.Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(nameof(UpdateWorkerPreferences))
            .WithTags("Workers");
    }

    private static async Task<IResult> UpdateAsync(
        Body body,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var workerUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(
            new UpdateWorkerPreferences.Request(workerUserId, body.InterestedFields, body.PreferredShiftTypes),
            cancellationToken);

        return Results.Ok(response);
    }
}
