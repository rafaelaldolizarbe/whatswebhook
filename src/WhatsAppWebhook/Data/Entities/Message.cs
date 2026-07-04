namespace WhatsAppWebhook.Data.Entities;

public enum MessageDirection
{
    Inbound,
    Outbound
}

public class Message
{
    public int Id { get; set; }

    public required string WamId { get; set; }

    public int ContactId { get; set; }

    public Contact? Contact { get; set; }

    public MessageDirection Direction { get; set; }

    public required string Type { get; set; }

    public string? Body { get; set; }

    public string? Status { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    public required string RawPayload { get; set; }
}
