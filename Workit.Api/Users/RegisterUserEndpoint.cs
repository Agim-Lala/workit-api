using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Users;

namespace Workit.Api.Users;

public sealed class RegisterUserEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", RegisterAsync)
            .AllowAnonymous()
            .WithName(nameof(RegisterUser))
            .WithTags("Auth");
    }

    private static async Task<IResult> RegisterAsync(
        RegisterUser.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(request, cancellationToken);
        return Results.Created($"/users/{response.User.Id}", response);
    }
}
