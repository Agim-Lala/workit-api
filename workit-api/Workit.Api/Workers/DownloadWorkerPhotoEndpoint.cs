using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

public sealed class DownloadWorkerPhotoEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/worker-profile/photo", DownloadAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg")
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(DownloadWorkerPhoto))
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

        var response = await mediator.Send(new DownloadWorkerPhoto.Request(workerUserId), cancellationToken);
        return Results.File(response.Content, response.ContentType);
    }
}
