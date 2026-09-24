using MediatR;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Routing;
using Workit.Core.Geocoding;
using Workit.Core.Shared.Location;

namespace Workit.Api.Geocoding;

public sealed class AutocompleteAddressEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        // Anonymous because it backs the business signup form, before the user has an account.
        app.MapGet("/geocoding/autocomplete", GetAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Geocoding)
            .Produces<IReadOnlyList<AddressSuggestion>>()
            .Produces(StatusCodes.Status400BadRequest)
            .WithName(nameof(AutocompleteAddress))
            .WithTags("Geocoding");
    }

    private static async Task<IResult> GetAsync(
        string q,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        return Results.Ok(await mediator.Send(new AutocompleteAddress.Request(q), cancellationToken));
    }
}
