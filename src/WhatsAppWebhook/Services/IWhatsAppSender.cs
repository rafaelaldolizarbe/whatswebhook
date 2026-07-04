using WhatsAppWebhook.Data.Entities;

namespace WhatsAppWebhook.Services;

public interface IWhatsAppSender
{
    Task SendTextAsync(Contact contact, string body, CancellationToken ct = default);

    Task SendButtonsAsync(Contact contact, string bodyText, IReadOnlyList<(string Id, string Title)> buttons, CancellationToken ct = default);

    Task SendListAsync(Contact contact, string bodyText, string buttonLabel, IReadOnlyList<(string Id, string Title, string? Description)> rows, CancellationToken ct = default);
}
