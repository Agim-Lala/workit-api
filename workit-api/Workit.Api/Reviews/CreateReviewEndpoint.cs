using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Reviews;

namespace Workit.Api.Reviews;

public sealed class CreateReviewEndpoint : IRouteMapper
{
    public sealed record Body(Guid JobAssignmentId, int Rating, string? Comment);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/reviews", CreateAsync)
            .RequireAuthorization()
            .Produces<CreateReview.Response>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(CreateReview))
            .WithTags("Reviews");
    }

    private static async Task<IResult> CreateAsync(
        Body request,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var callerUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(
            new CreateReview.Request(callerUserId, request.JobAssignmentId, request.Rating, request.Comment),
            cancellationToken);
        return Results.Created($"/reviews/{response.Id}", response);
    }
}
