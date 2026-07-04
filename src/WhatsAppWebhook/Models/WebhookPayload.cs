using System.Text.Json.Serialization;

namespace WhatsAppWebhook.Models;

// Schema conforme documentação oficial da Meta (Cloud API > Webhooks > Messages).
// Apenas os campos usados nesta fase estão mapeados.

public class WebhookPayload
{
    [JsonPropertyName("object")]
    public string? Object { get; set; }

    [JsonPropertyName("entry")]
    public List<WebhookEntry> Entry { get; set; } = [];
}

public class WebhookEntry
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("changes")]
    public List<WebhookChange> Changes { get; set; } = [];
}

public class WebhookChange
{
    [JsonPropertyName("value")]
    public WebhookValue? Value { get; set; }

    [JsonPropertyName("field")]
    public string? Field { get; set; }
}

public class WebhookValue
{
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; set; }

    [JsonPropertyName("contacts")]
    public List<WebhookContact>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WebhookMessage>? Messages { get; set; }

    [JsonPropertyName("statuses")]
    public List<WebhookStatus>? Statuses { get; set; }
}

public class WebhookContact
{
    [JsonPropertyName("profile")]
    public WebhookProfile? Profile { get; set; }

    [JsonPropertyName("wa_id")]
    public string? WaId { get; set; }
}

public class WebhookProfile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class WebhookMessage
{
    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("text")]
    public WebhookText? Text { get; set; }

    [JsonPropertyName("interactive")]
    public WebhookInteractive? Interactive { get; set; }
}

public class WebhookText
{
    [JsonPropertyName("body")]
    public string? Body { get; set; }
}

public class WebhookInteractive
{
    [JsonPropertyName("button_reply")]
    public WebhookInteractiveReply? ButtonReply { get; set; }

    [JsonPropertyName("list_reply")]
    public WebhookInteractiveReply? ListReply { get; set; }
}

public class WebhookInteractiveReply
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class WebhookStatus
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("recipient_id")]
    public string? RecipientId { get; set; }
}
