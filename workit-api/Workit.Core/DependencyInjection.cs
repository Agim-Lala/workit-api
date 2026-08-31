using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Workit.Core.Shared.Behaviours;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Persistence.DataMigrators;
using Workit.Core.Shared.Persistence.DataSeeders;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Persistence.GenericQueries;
using Workit.Core.Shared.Resiliency;
using Workit.Core.Shared.Services;
using Workit.Core.Shared.Time;
using Workit.Core.Shared.Tokens;

namespace Workit.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
        services.AddInternalServices();

        services.AddScoped<IDataWriter, EfDataWriter>();
        services.AddScoped<IGenericQuery, EfGenericQuery>();
        services.AddScoped<IDataMigrator, EfDataMigrator>();
        services.AddScoped<IDataSeeder, WorkitDataSeeder>();
        services.AddScoped<IResilienceHandler, PollyResilienceHandler>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddTransient<IAccessTokenCreator, JwtAccessTokenCreator>();
        services.AddTransient<ITokenService, TokenService>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    private static IServiceCollection AddInternalServices(this IServiceCollection services)
    {
        var serviceTypes = new[]
        {
            typeof(IService<>),
            typeof(IService<,>)
        };

        var implementations = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .Select(type => new
            {
                ImplementationType = type,
                ServiceInterfaces = type.GetInterfaces()
                    .Where(serviceInterface => serviceInterface.IsGenericType
                        && serviceTypes.Contains(serviceInterface.GetGenericTypeDefinition()))
                    .ToArray()
            })
            .Where(item => item.ServiceInterfaces.Length > 0);

        foreach (var implementation in implementations)
        {
            foreach (var serviceInterface in implementation.ServiceInterfaces)
            {
                services.AddScoped(serviceInterface, implementation.ImplementationType);
            }
        }

        return services;
    }
}
