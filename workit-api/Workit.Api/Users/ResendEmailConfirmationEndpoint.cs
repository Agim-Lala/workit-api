using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Users;

namespace Workit.Api.Users;

public sealed class ResendEmailConfirmationEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/resend-confirmation", ResendAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Produces(StatusCodes.Status200OK)
            .WithName(nameof(ResendEmailConfirmation))
            .WithTags("Auth");
    }

    private static async Task<IResult> ResendAsync(
        ResendEmailConfirmation.Request request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        await mediator.Send(request, cancellationToken);
        return Results.Ok();
    }
}
