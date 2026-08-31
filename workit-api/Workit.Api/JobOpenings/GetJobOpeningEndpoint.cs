using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.JobOpenings;

namespace Workit.Api.JobOpenings;

public sealed class GetJobOpeningEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/job-openings/{id:guid}", GetAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            .Produces<GetJobOpening.Response>()
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(GetJobOpening))
            .WithTags("Job Openings");
    }

    private static async Task<GetJobOpening.Response> GetAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetJobOpening.Request(id), cancellationToken);
    }
}
