using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.IdentityVerification;
using Workit.Core.Shared.Time;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

/// <summary>
/// Receives Persona's inquiry-outcome webhook and applies it to the matching worker's
/// verification badge. Anonymous at the ASP.NET Core auth layer (Persona cannot present a
/// Workit JWT) but every request must carry a valid <c>Persona-Signature</c> header, checked
/// against <c>PERSONA_WEBHOOK_SECRET</c> before anything in the body is trusted.
/// </summary>
public sealed class PersonaWebhookEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/persona", HandleAsync)
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName("PersonaWebhook")
            .WithTags("Webhooks");
    }

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        WorkitSettings settings,
        IClock clock,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var webhookSecret = settings.Persona.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return Results.Unauthorized();
        }

        using var reader = new StreamReader(context.Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var signatureHeader = context.Request.Headers["Persona-Signature"].ToString();

        if (!PersonaWebhookSignature.IsValid(signatureHeader, rawBody, webhookSecret, clock.UtcNow))
        {
            return Results.Unauthorized();
        }

        var webhookEvent = PersonaWebhookPayloadParser.TryParse(rawBody);
        if (webhookEvent is null)
        {
            return Results.BadRequest();
        }

        await mediator.Send(
            new HandlePersonaWebhook.Request(webhookEvent.InquiryId, webhookEvent.Status),
            cancellationToken);

        return Results.Ok();
    }
}
