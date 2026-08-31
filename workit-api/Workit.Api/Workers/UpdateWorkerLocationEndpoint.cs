using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

public sealed class UpdateWorkerLocationEndpoint : IRouteMapper
{
    public sealed record Body(string Location);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPut("/worker-profile/location", UpdateAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            .Produces<UpdateWorkerLocation.Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(nameof(UpdateWorkerLocation))
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
            new UpdateWorkerLocation.Request(workerUserId, body.Location),
            cancellationToken);

        return Results.Ok(response);
    }
}
