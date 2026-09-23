using System.Linq.Expressions;
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Workit.Core.Shared.Behaviours;
using Workit.Core.Shared.Email;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.IdentityVerification;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Location;
using Workit.Core.Shared.PasswordHashers;
using Workit.Core.Shared.Persistence.DataMigrators;
using Workit.Core.Shared.Persistence.DataSeeders;
using Workit.Core.Shared.Persistence.DataWriters;
using Workit.Core.Shared.Persistence.GenericQueries;
using Workit.Core.Shared.Resiliency;
using Workit.Core.Shared.Services;
using Workit.Core.Shared.Storage;
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

        var localizer = new JsonLocalizer();
        services.AddSingleton<ILocalizer>(localizer);
        ConfigureValidatorLocalization(localizer);

        services.AddScoped<IDataWriter, EfDataWriter>();
        services.AddScoped<IGenericQuery, EfGenericQuery>();
        services.AddScoped<IDataMigrator, EfDataMigrator>();
        services.AddScoped<IDataSeeder, WorkitDataSeeder>();
        services.AddScoped<IResilienceHandler, PollyResilienceHandler>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddTransient<IAccessTokenCreator, JwtAccessTokenCreator>();
        services.AddTransient<ITokenService, TokenService>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IFileStorage, LocalDiskFileStorage>();
        services.AddSingleton<IEmailConfirmationQueue, EmailConfirmationQueue>();
        services.AddHttpClient<IEmailSender, ResendEmailSender>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com/");
        });

        services.AddHttpClient<ICityLookupService, NominatimCityLookupService>(client =>
        {
            client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
            // Nominatim's usage policy requires a descriptive, reachable User-Agent; update the
            // contact before relying on this in production (see nominatim.org/release-docs/latest/api/Usage-Policy).
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WorkitApp/1.0 (+https://workit.example; contact: support@workit.example)");
        });

        services.AddHttpClient<IIdentityVerificationProvider, StripeIdentityVerificationProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.stripe.com/v1/");
        });

        return services;
    }

    private static void ConfigureValidatorLocalization(ILocalizer localizer)
    {
        ValidatorOptions.Global.LanguageManager = new WorkitValidatorLanguageManager(localizer);
        ValidatorOptions.Global.DisplayNameResolver = ResolveDisplayName;

        string? ResolveDisplayName(Type type, MemberInfo? member, LambdaExpression? expression)
        {
            if (member is null)
            {
                return null;
            }

            var key = $"property.{member.Name}";
            var translated = localizer.Translate(key);
            return translated == key ? null : translated;
        }
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
