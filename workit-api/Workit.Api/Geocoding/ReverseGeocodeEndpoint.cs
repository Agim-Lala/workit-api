using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Geocoding;
using Workit.Core.Shared.Location;

namespace Workit.Api.Geocoding;

public sealed class ReverseGeocodeEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        // Anonymous because it backs the business signup form, before the user has an account.
        app.MapGet("/geocoding/reverse", GetAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Geocoding)
            .Produces<AddressSuggestion>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName(nameof(ReverseGeocode))
            .WithTags("Geocoding");
    }

    private static async Task<IResult> GetAsync(
        double lat,
        double lon,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await mediator.Send(new ReverseGeocode.Request(lat, lon), cancellationToken));
    }
}
