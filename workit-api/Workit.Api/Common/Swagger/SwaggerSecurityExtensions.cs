using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Workit.Api.Common.Swagger;

public static class SwaggerSecurityExtensions
{
    public static void AddBearerSecurity(this SwaggerGenOptions options)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "JWT bearer token",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        options.AddSecurityDefinition("Bearer", scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [scheme] = Array.Empty<string>()
        });
    }
}
