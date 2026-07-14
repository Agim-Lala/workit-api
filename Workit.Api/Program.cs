using System.Text;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Workit.Api.Common.Persistence;
using Workit.Api.Common.Routing;
using Workit.Api.Common.Swagger;
using Workit.Core;
using Workit.Core.Shared.EnvironmentUtils;
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

builder.Services.AddAuthorization();

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

app.UseSerilogRequestLogging();
app.UseExceptionHandler(exceptionApp => exceptionApp.Run(ApiExceptionWriter.WriteAsync));

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

if (settings.Hangfire.Enabled)
{
    app.UseHangfireDashboard("/jobs");
}

app.MapRoutes();

app.Run();

public partial class Program;
