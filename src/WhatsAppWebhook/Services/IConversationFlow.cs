using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Models;

namespace WhatsAppWebhook.Services;

// Isolado atrás de interface porque será substituído/estendido pela camada de IA
// no futuro (seção 8 do HERE.md) sem precisar mexer no MessageProcessor.
public interface IConversationFlow
{
    Task HandleAsync(Contact contact, WebhookMessage message, CancellationToken ct);
}
