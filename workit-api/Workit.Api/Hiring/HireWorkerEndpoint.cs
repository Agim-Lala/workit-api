using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Hiring;

namespace Workit.Api.Hiring;

public sealed class HireWorkerEndpoint : IRouteMapper
{
    public sealed record Body(Guid WorkerProfileId);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/job-openings/{jobOpeningId:guid}/assignments", HireAsync)
            .RequireAuthorization(AuthorizationPolicies.BusinessOnly)
            .Produces<HireWorker.Response>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(HireWorker))
            .WithTags("Hiring");
    }

    private static async Task<IResult> HireAsync(
        Guid jobOpeningId,
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
            new HireWorker.Request(businessUserId, jobOpeningId, request.WorkerProfileId),
            cancellationToken);
        return Results.Created($"/assignments/{response.Id}", response);
    }
}
