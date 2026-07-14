using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Users;

namespace Workit.Api.Users;

public sealed class LoginUserEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", LoginAsync)
            .AllowAnonymous()
            .WithName(nameof(LoginUser))
            .WithTags("Auth");
    }

    private static async Task<IResult> LoginAsync(
        LoginUser.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await mediator.Send(request, cancellationToken));
    }
}
