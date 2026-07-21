namespace WhatsAppWebhook.Data.Entities;

public class Contact
{
    public int Id { get; set; }

    public required string WaId { get; set; }

    public string? ProfileName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    // Quando true, o MessageProcessor não chama o IConversationFlow pra esse
    // contato — um humano assumiu a conversa pelo painel administrativo.
    public bool HumanTakeoverEnabled { get; set; }

    // Fica true quando o contato clica "Falar com o Rafael" — some quando o
    // admin abre a conversa (ver ConversationComponent/dismiss-attention).
    public bool NeedsHumanAttention { get; set; }

    public List<Message> Messages { get; set; } = [];
}
