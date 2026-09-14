using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

public sealed class StartWorkerVerificationEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/worker-profile/verification/start", StartAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            .Produces<StartWorkerVerification.Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(nameof(StartWorkerVerification))
            .WithTags("Workers");
    }

    private static async Task<IResult> StartAsync(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var workerUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(new StartWorkerVerification.Request(workerUserId), cancellationToken);
        return Results.Ok(response);
    }
}
