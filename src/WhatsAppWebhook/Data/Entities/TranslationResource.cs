namespace WhatsAppWebhook.Data.Entities;

// Alimenta o site Next.js (rfchdev.com.br) via /api/translations/{locale} —
// ver docs/publish-deploy.md e translations-api-backend.md pro contrato original.
public class TranslationResource
{
    public int Id { get; set; }

    public required string Locale { get; set; } // pt | en | es

    public required string Namespace { get; set; } // nav | hero | about | ...

    // JSON livre — texto bruto validado no endpoint antes de salvar, não em struct fixa.
    public required string ContentJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
