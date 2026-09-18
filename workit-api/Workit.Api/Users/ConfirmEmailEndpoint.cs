using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Users;

namespace Workit.Api.Users;

public sealed class ConfirmEmailEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/confirm-email", ConfirmAsync)
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName(nameof(ConfirmEmail))
            .WithTags("Auth");
    }

    private static async Task<IResult> ConfirmAsync(
        ConfirmEmail.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(request, cancellationToken);
        return Results.Ok();
    }
}
