using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

public sealed class GetTopWorkersEndpoint : IRouteMapper
{
    public sealed record Query(int Limit = 20);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/job-openings/{jobOpeningId:guid}/top-workers", GetAsync)
            .RequireAuthorization(AuthorizationPolicies.BusinessOnly)
            .Produces<GetTopWorkers.Response>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(GetTopWorkers))
            .WithTags("Workers");
    }

    private static async Task<IResult> GetAsync(
        Guid jobOpeningId,
        [AsParameters] Query query,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var businessUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(
            new GetTopWorkers.Request(businessUserId, jobOpeningId, query.Limit),
            cancellationToken);
        return Results.Ok(response);
    }
}
