using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.JobOpenings;

namespace Workit.Api.JobOpenings;

public sealed class CreateJobOpeningEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/job-openings", CreateAsync)
            .RequireAuthorization()
            .Produces<CreateJobOpening.Response>(StatusCodes.Status201Created)
            .WithName(nameof(CreateJobOpening))
            .WithTags("Job Openings");
    }

    private static async Task<IResult> CreateAsync(
        CreateJobOpening.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(request, cancellationToken);
        return Results.Created($"/job-openings/{response.Id}", response);
    }
}
