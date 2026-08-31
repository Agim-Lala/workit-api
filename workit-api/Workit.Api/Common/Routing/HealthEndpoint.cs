namespace Workit.Api.Common.Routing;

public sealed class HealthEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health")
            .AllowAnonymous()
            .WithName("Health")
            .WithTags("Health");
    }
}
