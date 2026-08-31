namespace Workit.Api.Common.Routing;

public sealed class ApiInfoEndpoints : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/", () => Results.Ok(new
        {
            name = "Workit API",
            status = "running",
            health = "/health",
            swagger = "/swagger"
        }))
            .AllowAnonymous()
            .WithName("ApiInfo")
            .WithTags("Info");
    }
}
