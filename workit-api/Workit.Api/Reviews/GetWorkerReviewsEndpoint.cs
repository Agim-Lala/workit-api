using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Reviews;

namespace Workit.Api.Reviews;

public sealed class GetWorkerReviewsEndpoint : IRouteMapper
{
    public sealed record Query(int Page = 1, int PageSize = 25);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/workers/{workerProfileId:guid}/reviews", GetAsync)
            .RequireAuthorization()
            .Produces<GetWorkerReviews.Response>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(GetWorkerReviews))
            .WithTags("Reviews");
    }

    private static async Task<IResult> GetAsync(
        Guid workerProfileId,
        [AsParameters] Query query,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new GetWorkerReviews.Request(workerProfileId, query.Page, query.PageSize),
            cancellationToken);
        return Results.Ok(response);
    }
}
