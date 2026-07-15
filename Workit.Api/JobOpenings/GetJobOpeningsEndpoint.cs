using MediatR;
using Microsoft.AspNetCore.Mvc;
using Workit.Api.Common.Routing;
using Workit.Core.JobOpenings;

namespace Workit.Api.JobOpenings;

public sealed class GetJobOpeningsEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/job-openings", GetAsync)
            .RequireAuthorization()
            .Produces<GetJobOpenings.Response>()
            .WithName(nameof(GetJobOpenings))
            .WithTags("Job Openings");
    }

    private static async Task<GetJobOpenings.Response> GetAsync(
        [AsParameters] GetJobOpenings.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(request, cancellationToken);
    }
}
