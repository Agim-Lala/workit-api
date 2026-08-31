using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

public sealed class GetWorkerProfileEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/worker-profile", GetAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            .Produces<GetWorkerProfile.Response>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(nameof(GetWorkerProfile))
            .WithTags("Workers");
    }

    private static async Task<IResult> GetAsync(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var workerUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(
            new GetWorkerProfile.Request(workerUserId),
            cancellationToken);

        return Results.Ok(response);
    }
}
