using System.Security.Claims;
using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

public sealed class UploadWorkerCvEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/worker-profile/cv", UploadAsync)
            .RequireAuthorization(AuthorizationPolicies.WorkerOnly)
            // JWT-bearer API, not a cookie/browser form post — no antiforgery middleware is wired up.
            .DisableAntiforgery()
            .Produces<UploadWorkerCv.Response>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName(nameof(UploadWorkerCv))
            .WithTags("Workers");
    }

    private static async Task<IResult> UploadAsync(
        IFormFile file,
        ClaimsPrincipal user,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var workerUserId))
        {
            return Results.Unauthorized();
        }

        await using var stream = file.OpenReadStream();
        var response = await mediator.Send(
            new UploadWorkerCv.Request(workerUserId, stream, file.FileName, file.ContentType, file.Length),
            cancellationToken);

        return Results.Ok(response);
    }
}
