using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;
using Workit.Core.Shared.Exceptions;
using Workit.Core.Shared.Localization;

namespace Workit.Api.Common.Routing;

public static class ApiExceptionWriter
{
    public static async Task WriteAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var environment = context.RequestServices.GetRequiredService<IHostEnvironment>();
        var localizer = context.RequestServices.GetRequiredService<ILocalizer>();

        (int statusCode, object payload) = exception switch
        {
            RequestValidationException validationException => (StatusCodes.Status400BadRequest, (object)new
            {
                title = localizer["error.title.validation"],
                errors = validationException.Errors
            }),
            NotFoundException notFoundException => (StatusCodes.Status404NotFound, (object)new
            {
                title = localizer["error.title.notFound"],
                detail = Localize(localizer, notFoundException)
            }),
            DomainException domainException => (StatusCodes.Status400BadRequest, (object)new
            {
                title = localizer["error.title.domain"],
                detail = Localize(localizer, domainException)
            }),
            _ => (StatusCodes.Status500InternalServerError, (object)new
            {
                title = localizer["error.title.unexpected"],
                detail = environment.IsDevelopment() && exception is not null
                    ? exception.Message
                    : localizer["error.unexpected"]
            })
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(payload);
    }

    private static string Localize(ILocalizer localizer, ILocalizableError error) =>
        localizer.Translate(error.LocalizationKey, error.LocalizationArgs);
}
