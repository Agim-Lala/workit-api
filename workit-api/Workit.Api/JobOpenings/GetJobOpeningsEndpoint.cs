using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.JobOpenings;
using Workit.Core.JobOpenings.Domain;

namespace Workit.Api.JobOpenings;

public sealed class GetJobOpeningsEndpoint : IRouteMapper
{
    public sealed record Query(
        int Page = 1,
        int PageSize = 25,
        Guid? BusinessProfileId = null,
        JobOpeningStatus? Status = null,
        JobType? JobType = null,
        DateOnly? OnDate = null,
        ShiftType? ShiftType = null);

    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/job-openings", GetAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            .Produces<GetJobOpenings.Response>()
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(nameof(GetJobOpenings))
            .WithTags("Job Openings");
    }

    private static async Task<IResult> GetAsync(
        [AsParameters] Query query,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var workerUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(
            new GetJobOpenings.Request(
                workerUserId,
                query.Page,
                query.PageSize,
                query.BusinessProfileId,
                query.Status,
                query.JobType,
                query.OnDate,
                query.ShiftType),
            cancellationToken);

        return Results.Ok(response);
    }
}
