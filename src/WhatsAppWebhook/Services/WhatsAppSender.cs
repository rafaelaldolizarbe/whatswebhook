using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Dtos;
using WhatsAppWebhook.Hubs;

namespace WhatsAppWebhook.Services;

// Envio via Graph API (seção 4.4). Regra da janela de 24h: mensagens de formato
// livre só podem ser enviadas até 24h após a última mensagem do usuário. Nesta
// fase o bot é sempre reativo (responde dentro da janela), então templates
// aprovados para fora da janela não são implementados ainda.
public class WhatsAppSender(HttpClient httpClient, AppDbContext db, IConfiguration configuration, IHubContext<ConversationHub> hub, ILogger<WhatsAppSender> logger) : IWhatsAppSender
{
    public Task SendTextAsync(Contact contact, string body, CancellationToken ct = default)
        => SendAsync(contact, "text", new { text = new { body } }, body, ct);

    public Task SendButtonsAsync(Contact contact, string bodyText, IReadOnlyList<(string Id, string Title)> buttons, CancellationToken ct = default)
    {
        var interactive = new
        {
            type = "button",
            body = new { text = bodyText },
            action = new
            {
                buttons = buttons.Select(b => new
                {
                    type = "reply",
                    reply = new { id = b.Id, title = b.Title }
                }).ToArray()
            }
        };

        return SendAsync(contact, "interactive", new { interactive }, JsonSerializer.Serialize(interactive), ct);
    }

    public Task SendListAsync(Contact contact, string bodyText, string buttonLabel, IReadOnlyList<(string Id, string Title, string? Description)> rows, CancellationToken ct = default)
    {
        var interactive = new
        {
            type = "list",
            body = new { text = bodyText },
            action = new
            {
                button = buttonLabel,
                sections = new[]
                {
                    new
                    {
                        title = "Opções",
                        rows = rows.Select(r => new { id = r.Id, title = r.Title, description = r.Description }).ToArray()
                    }
                }
            }
        };

        return SendAsync(contact, "interactive", new { interactive }, JsonSerializer.Serialize(interactive), ct);
    }

    private async Task SendAsync(Contact contact, string type, object typeSpecificContent, string bodyToPersist, CancellationToken ct)
    {
        var phoneNumberId = configuration["META_PHONE_NUMBER_ID"];
        var accessToken = configuration["META_ACCESS_TOKEN"];
        var version = configuration["META_GRAPH_VERSION"] ?? "v21.0";
        // META_GRAPH_BASE_URL é opcional, só existe para apontar a um servidor
        // local durante testes/desenvolvimento; em produção nunca é definido.
        var baseUrl = configuration["META_GRAPH_BASE_URL"] ?? "https://graph.facebook.com";

        var payload = MergePayload(contact.WaId, type, typeSpecificContent);

        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/{version}/{phoneNumberId}/messages")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        HttpResponseMessage response;
        string responseBody;
        try
        {
            response = await httpClient.SendAsync(request, ct);
            responseBody = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha de rede ao chamar a Graph API para {WaId}", contact.WaId);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Graph API retornou {Status} ao enviar para {WaId}: {Body}", response.StatusCode, contact.WaId, responseBody);
            return;
        }

        var wamId = ExtractWamId(responseBody);
        if (wamId is null)
        {
            logger.LogError("Resposta da Graph API sem id de mensagem: {Body}", responseBody);
            return;
        }

        var sentMessage = new Message
        {
            WamId = wamId,
            ContactId = contact.Id,
            Direction = MessageDirection.Outbound,
            Type = type,
            Body = bodyToPersist,
            Timestamp = DateTimeOffset.UtcNow,
            RawPayload = JsonSerializer.Serialize(payload)
        };
        db.Messages.Add(sentMessage);
        await db.SaveChangesAsync(ct);

        var summary = new MessageSummary(sentMessage.Id, sentMessage.Direction, sentMessage.Type, sentMessage.Body, sentMessage.Status, sentMessage.Timestamp);
        await hub.Clients.Group(ConversationHub.GroupName(contact.Id)).SendAsync("messageReceived", summary, ct);
    }

    private static Dictionary<string, object?> MergePayload(string to, string type, object typeSpecificContent)
    {
        var payload = new Dictionary<string, object?>
        {
            ["messaging_product"] = "whatsapp",
            ["to"] = to,
            ["type"] = type
        };

        foreach (var prop in typeSpecificContent.GetType().GetProperties())
        {
            payload[prop.Name] = prop.GetValue(typeSpecificContent);
        }

        return payload;
    }

    private static string? ExtractWamId(string responseBody)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("messages")[0].GetProperty("id").GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
