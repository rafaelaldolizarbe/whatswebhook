using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;

namespace WhatsAppWebhook.Endpoints;

public static class WebhookEndpoints
{
    public static void MapWebhookEndpoints(this WebApplication app)
    {
        app.MapGet("/webhook", HandleVerification);
        app.MapPost("/webhook", HandleIncomingEvent);
    }

    public static IResult HandleVerification(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge,
        IConfiguration configuration)
    {
        var expectedToken = configuration["META_VERIFY_TOKEN"];
        var result = WebhookVerifier.Verify(mode, verifyToken, challenge, expectedToken);

        return result.IsValid
            ? Results.Text(result.Challenge, "text/plain")
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    public static async Task<IResult> HandleIncomingEvent(
        HttpRequest request,
        ChannelWriter<string> queueWriter,
        IConfiguration configuration,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger("WhatsAppWebhook.Endpoints.WebhookEndpoints");

        // Regra obrigatória (seção 4.2): responder 200 rápido e nunca deixar
        // uma exceção escapar, senão a Meta reenvia o evento indefinidamente.
        try
        {
            using var reader = new StreamReader(request.Body);
            var rawBody = await reader.ReadToEndAsync();

            var signature = request.Headers["X-Hub-Signature-256"].ToString();
            var appSecret = configuration["META_APP_SECRET"];

            if (!WebhookSignatureValidator.IsValid(rawBody, signature, appSecret))
            {
                logger.LogWarning("Assinatura X-Hub-Signature-256 ausente ou inválida");
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            await queueWriter.WriteAsync(rawBody);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro inesperado ao receber evento em POST /webhook");
        }

        return Results.Ok();
    }
}
