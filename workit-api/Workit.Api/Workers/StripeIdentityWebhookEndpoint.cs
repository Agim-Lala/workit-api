using MediatR;
using Workit.Api.Common.Routing;
using Workit.Core.Shared.EnvironmentUtils;
using Workit.Core.Shared.IdentityVerification;
using Workit.Core.Shared.Time;
using Workit.Core.Workers;

namespace Workit.Api.Workers;

/// <summary>
/// Receives Stripe Identity's VerificationSession webhook and applies it to the matching
/// worker's verification badge. Anonymous at the ASP.NET Core auth layer (Stripe cannot present
/// a Workit JWT) but every request must carry a valid <c>Stripe-Signature</c> header, checked
/// against <c>STRIPE_WEBHOOK_SECRET</c> before anything in the body is trusted.
/// </summary>
public sealed class StripeIdentityWebhookEndpoint : IRouteMapper
{
    public void MapRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/webhooks/stripe-identity", HandleAsync)
            .AllowAnonymous()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .WithName("StripeIdentityWebhook")
            .WithTags("Webhooks");
    }

    private static async Task<IResult> HandleAsync(
        HttpContext context,
        WorkitSettings settings,
        IClock clock,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var webhookSecret = settings.StripeIdentity.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            return Results.Unauthorized();
        }

        using var reader = new StreamReader(context.Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var signatureHeader = context.Request.Headers["Stripe-Signature"].ToString();

        if (!StripeWebhookSignature.IsValid(signatureHeader, rawBody, webhookSecret, clock.UtcNow))
        {
            return Results.Unauthorized();
        }

        var webhookEvent = StripeWebhookPayloadParser.TryParse(rawBody);
        if (webhookEvent is null)
        {
            return Results.BadRequest();
        }

        await mediator.Send(
            new HandleStripeIdentityWebhook.Request(webhookEvent.EventType, webhookEvent.VerificationSessionId),
            cancellationToken);

        return Results.Ok();
    }
}
