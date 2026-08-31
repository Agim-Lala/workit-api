using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Workers;

namespace Workit.Api.Users;

public sealed class RegisterWorkerEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register/worker", RegisterAsync)
            .AllowAnonymous()
            .Produces<RegisterWorker.Response>(StatusCodes.Status201Created)
            .WithName(nameof(RegisterWorker))
            .WithTags("Auth");
    }

    private static async Task<IResult> RegisterAsync(
        RegisterWorker.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(request, cancellationToken);
        return Results.Created($"/users/{response.User.Id}", response);
    }
}
