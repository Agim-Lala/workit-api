using System.Globalization;
using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Workit.Api.Common.Auth;
using Workit.Api.Common.Persistence;
using Workit.Api.Common.Routing;
using Workit.Api.Common.Swagger;
using Workit.Core;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.Localization;
using Workit.Core.Shared.Persistence;

var builder = WebApplication.CreateBuilder(args);
var settings = WorkitSettings.FromEnvironment();

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

if (!string.IsNullOrWhiteSpace(settings.Sentry.Dsn))
{
    builder.WebHost.UseSentry(options =>
    {
        options.Dsn = settings.Sentry.Dsn;
        options.TracesSampleRate = settings.Sentry.TracesSampleRate;
        options.Environment = settings.Sentry.Environment;
    });
}

builder.Services.AddSingleton(settings);
builder.Services.AddCoreServices();
builder.Services.AddHealthChecks();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Workit API", Version = "v1" });
    options.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    options.AddBearerSecurity();
});

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var cultures = Language.Supported.Select(language => new CultureInfo(language)).ToArray();
    options.DefaultRequestCulture = new RequestCulture(Language.Default);
    options.SupportedCultures = cultures;
    options.SupportedUICultures = cultures;
    options.ApplyCurrentCultureToResponseHeaders = true;
    options.RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()];
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(settings.Cors.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Token.Secret))
        };
    });

builder.Services.AddAuthorization(AuthorizationPolicies.AddWorkitPolicies);

builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        return new ValueTask();
    };

    options.AddPolicy(RateLimitPolicies.Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = settings.RateLimit.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.RateLimit.WindowInSeconds),
            QueueLimit = 0
        }));
});

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(settings.Database.MainConnectionString);
});

builder.Services.AddDbContext<ReadAppDbContext>(options =>
{
    options.UseNpgsql(settings.Database.ReadConnectionString ?? settings.Database.MainConnectionString);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});

if (settings.Hangfire.Enabled)
{
    builder.Services.AddHangfire(config => config
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseMemoryStorage());
    builder.Services.AddHangfireServer();
}

var app = builder.Build();

await app.ApplyMigrationsAndSeedDataAsync();

app.UseRequestLocalization();
app.UseSerilogRequestLogging();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(ApiExceptionWriter.WriteAsync));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (settings.Hangfire.Enabled)
{
    app.UseHangfireDashboard("/jobs");
}

app.MapRoutes();

app.Run();

public partial class Program;
