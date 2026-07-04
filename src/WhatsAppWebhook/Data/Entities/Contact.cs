namespace WhatsAppWebhook.Data.Entities;

public class Contact
{
    public int Id { get; set; }

    public required string WaId { get; set; }

    public string? ProfileName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public List<Message> Messages { get; set; } = [];
}
