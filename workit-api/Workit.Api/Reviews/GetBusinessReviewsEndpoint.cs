using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Reviews;

namespace Workit.Api.Reviews;

public sealed class GetBusinessReviewsEndpoint : IRouteMapper
{
    public sealed record Query(int Page = 1, int PageSize = 25);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/businesses/{businessProfileId:guid}/reviews", GetAsync)
            .RequireAuthorization()
            .Produces<GetBusinessReviews.Response>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(GetBusinessReviews))
            .WithTags("Reviews");
    }

    private static async Task<IResult> GetAsync(
        Guid businessProfileId,
        [AsParameters] Query query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new GetBusinessReviews.Request(businessProfileId, query.Page, query.PageSize),
            cancellationToken);
        return Results.Ok(response);
    }
}
