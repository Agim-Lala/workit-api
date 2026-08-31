using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Workit.Core.Shared.Exceptions;

namespace Workit.Api.Common.Routing;

public static class ApiExceptionWriter
{
    public static async Task WriteAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();

        (int statusCode, object payload) = exception switch
        {
            RequestValidationException validationException => (StatusCodes.Status400BadRequest, (object)new
            {
                title = "Validation failed",
                errors = validationException.Errors
            }),
            NotFoundException notFoundException => (StatusCodes.Status404NotFound, (object)new
            {
                title = "Not found",
                detail = notFoundException.Message
            }),
            DomainException domainException => (StatusCodes.Status400BadRequest, (object)new
            {
                title = "Domain error",
                detail = domainException.Message
            }),
            _ => (StatusCodes.Status500InternalServerError, (object)new
            {
                title = "Unexpected error",
                detail = environment.IsDevelopment() && exception is not null
                    ? exception.Message
                    : "An unexpected error occurred."
            })
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(payload);
    }
}
