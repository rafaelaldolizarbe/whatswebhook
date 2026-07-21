using WhatsAppWebhook.Data.Entities;

namespace WhatsAppWebhook.Dtos;

// Compartilhado entre AdminEndpoints (GET /admin/contacts/{id}/messages) e o
// broadcast do ConversationHub — mesma forma de dado nos dois casos.
public record MessageSummary(int Id, MessageDirection Direction, string Type, string? Body, string? Status, DateTimeOffset Timestamp);
