using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Businesses;

namespace Workit.Api.Users;

public sealed class RegisterBusinessEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register/business", RegisterAsync)
            .AllowAnonymous()
            .Produces<RegisterBusiness.Response>(StatusCodes.Status201Created)
            .WithName(nameof(RegisterBusiness))
            .WithTags("Auth");
    }

    private static async Task<IResult> RegisterAsync(
        RegisterBusiness.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(request, cancellationToken);
        return Results.Created($"/users/{response.User.Id}", response);
    }
}
