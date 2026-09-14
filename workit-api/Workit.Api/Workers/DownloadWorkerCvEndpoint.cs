using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

public sealed class DownloadWorkerCvEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/worker-profile/cv", DownloadAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(DownloadWorkerCv))
            .WithTags("Workers");
    }

    private static async Task<IResult> DownloadAsync(
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var workerUserId))
        {
            return Results.Unauthorized();
        }

        var response = await mediator.Send(new DownloadWorkerCv.Request(workerUserId), cancellationToken);
        return Results.File(response.Content, "application/pdf", response.FileName);
    }
}
