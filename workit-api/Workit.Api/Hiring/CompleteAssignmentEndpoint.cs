using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Hiring;

namespace Workit.Api.Hiring;

public sealed class CompleteAssignmentEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/assignments/{assignmentId:guid}/complete", CompleteAsync)
            .RequireAuthorization(AuthorizationPolicies.BusinessOnly)
            .Produces<CompleteAssignment.Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(CompleteAssignment))
            .WithTags("Hiring");
    }

    private static async Task<IResult> CompleteAsync(
        Guid assignmentId,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var businessUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(new CompleteAssignment.Request(businessUserId, assignmentId), cancellationToken);
        return Results.Ok(response);
    }
}
