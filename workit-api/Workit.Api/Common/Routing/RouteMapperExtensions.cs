using System.Reflection;

namespace Workit.Api.Common.Routing;

public static class RouteMapperExtensions
{
    public static IEndpointRouteBuilder MapRoutes(this IEndpointRouteBuilder app)
    {
        var routeMappers = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && typeof(IRouteMapper).IsAssignableFrom(type))
            .Select(type => (IRouteMapper)Activator.CreateInstance(type)!)
            .OrderBy(mapper => mapper.GetType().FullName);

        foreach (var routeMapper in routeMappers)
        {
            routeMapper.MapRoutes(app);
        }

        return app;
    }
}
