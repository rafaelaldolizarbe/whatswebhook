using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Dtos;
using WhatsAppWebhook.Hubs;
using WhatsAppWebhook.Models;

namespace WhatsAppWebhook.Services;

public class MessageProcessor(
    ChannelReader<string> reader,
    IServiceScopeFactory scopeFactory,
    IHubContext<ConversationHub> hub,
    ILogger<MessageProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var rawBody in reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAsync(rawBody, stoppingToken);
            }
            catch (Exception ex)
            {
                // Nunca deixamos uma falha de processamento derrubar o worker:
                // a Meta já recebeu 200 no POST, então só nos resta logar e seguir.
                logger.LogError(ex, "Falha ao processar evento de webhook em background");
            }
        }
    }

    private async Task ProcessAsync(string rawBody, CancellationToken ct)
    {
        WebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookPayload>(rawBody);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Payload de webhook não é um JSON válido, ignorando");
            return;
        }

        if (payload?.Entry is not { Count: > 0 })
        {
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var conversationFlow = scope.ServiceProvider.GetRequiredService<IConversationFlow>();

        foreach (var value in payload.Entry
                     .SelectMany(entry => entry.Changes)
                     .Select(change => change.Value)
                     .OfType<WebhookValue>())
        {
            foreach (var contactInfo in value.Contacts ?? [])
            {
                await UpsertContactAsync(db, contactInfo, ct);
            }

            foreach (var message in value.Messages ?? [])
            {
                await ProcessInboundMessageAsync(db, conversationFlow, message, ct);
            }

            foreach (var status in value.Statuses ?? [])
            {
                await ProcessStatusAsync(db, status, ct);
            }
        }
    }

    private static async Task<Contact?> UpsertContactAsync(AppDbContext db, WebhookContact contactInfo, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(contactInfo.WaId))
        {
            return null;
        }

        var contact = await db.Contacts.FirstOrDefaultAsync(c => c.WaId == contactInfo.WaId, ct);
        var now = DateTimeOffset.UtcNow;

        if (contact is null)
        {
            contact = new Contact
            {
                WaId = contactInfo.WaId,
                ProfileName = contactInfo.Profile?.Name,
                CreatedAt = now,
                LastSeenAt = now
            };
            db.Contacts.Add(contact);
        }
        else
        {
            contact.ProfileName = contactInfo.Profile?.Name ?? contact.ProfileName;
            contact.LastSeenAt = now;
        }

        await db.SaveChangesAsync(ct);
        return contact;
    }

    private async Task ProcessInboundMessageAsync(AppDbContext db, IConversationFlow conversationFlow, WebhookMessage message, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(message.Id) || string.IsNullOrEmpty(message.From))
        {
            logger.LogWarning("Mensagem inbound sem id ou remetente, ignorando");
            return;
        }

        var alreadyProcessed = await db.Messages.AnyAsync(m => m.WamId == message.Id, ct);
        if (alreadyProcessed)
        {
            logger.LogInformation("Mensagem {WamId} já processada anteriormente, ignorando (idempotência)", message.Id);
            return;
        }

        var contact = await UpsertContactAsync(db, new WebhookContact { WaId = message.From }, ct);
        if (contact is null)
        {
            return;
        }

        var body = message.Type == "text"
            ? message.Text?.Body
            : JsonSerializer.Serialize(message);

        var newMessage = new Message
        {
            WamId = message.Id,
            ContactId = contact.Id,
            Direction = MessageDirection.Inbound,
            Type = message.Type ?? "unknown",
            Body = body,
            Timestamp = ParseTimestamp(message.Timestamp),
            RawPayload = JsonSerializer.Serialize(message)
        };
        db.Messages.Add(newMessage);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            // Corrida entre dois eventos duplicados chegando quase ao mesmo tempo:
            // o índice único em WamId barra a segunda gravação.
            logger.LogInformation(ex, "Conflito de idempotência ao salvar {WamId}, ignorando", message.Id);
            return;
        }

        await BroadcastMessageAsync(contact.Id, newMessage, ct);

        if (contact.HumanTakeoverEnabled)
        {
            logger.LogInformation("Contato {WaId} está em atendimento humano, bot não responde", contact.WaId);
            return;
        }

        await conversationFlow.HandleAsync(contact, message, ct);
    }

    private async Task ProcessStatusAsync(AppDbContext db, WebhookStatus status, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(status.Id))
        {
            return;
        }

        var message = await db.Messages.FirstOrDefaultAsync(m => m.WamId == status.Id, ct);
        if (message is null)
        {
            logger.LogInformation("Status recebido para {WamId} que ainda não está no banco, ignorando", status.Id);
            return;
        }

        message.Status = status.Status;
        await db.SaveChangesAsync(ct);

        await hub.Clients.Group(ConversationHub.GroupName(message.ContactId))
            .SendAsync("messageStatusUpdated", new { messageId = message.Id, status = message.Status }, ct);
    }

    private Task BroadcastMessageAsync(int contactId, Message message, CancellationToken ct)
    {
        var summary = new MessageSummary(message.Id, message.Direction, message.Type, message.Body, message.Status, message.Timestamp);
        return hub.Clients.Group(ConversationHub.GroupName(contactId)).SendAsync("messageReceived", summary, ct);
    }

    private static DateTimeOffset ParseTimestamp(string? unixSeconds)
    {
        return long.TryParse(unixSeconds, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : DateTimeOffset.UtcNow;
    }
}
