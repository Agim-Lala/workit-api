using MediatR;
using Microsoft.AspNetCore.Mvc;
using Workit.Api.Common.Routing;
using Workit.Core.Users;

namespace Workit.Api.Users;

public sealed class GetAllUsersEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/users", GetAllAsync)
            .RequireAuthorization()
            .Produces<GetAllUsers.Response>()
            .WithName(nameof(GetAllUsers))
            .WithTags("Users");
    }

    private static async Task<GetAllUsers.Response> GetAllAsync(
        [AsParameters] GetAllUsers.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(request, cancellationToken);
    }
}
