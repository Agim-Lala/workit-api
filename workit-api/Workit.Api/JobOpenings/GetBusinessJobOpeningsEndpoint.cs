using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.JobOpenings;
using Workit.Core.JobOpenings.Domain;

namespace Workit.Api.JobOpenings;

public sealed class GetBusinessJobOpeningsEndpoint : IRouteMapper
{
    public sealed record Query(
        int Page = 1,
        int PageSize = 25,
        JobOpeningStatus? Status = null);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/business/job-openings", GetAsync)
            .RequireAuthorization(AuthorizationPolicies.BusinessOnly)
            .Produces<GetBusinessJobOpenings.Response>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(nameof(GetBusinessJobOpenings))
            .WithTags("Job Openings");
    }

    private static async Task<IResult> GetAsync(
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
            new GetBusinessJobOpenings.Request(
                businessUserId,
                query.Page,
                query.PageSize,
                query.Status),
            cancellationToken);

        return Results.Ok(response);
    }
}
