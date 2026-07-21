using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Hubs;
using WhatsAppWebhook.Models;

namespace WhatsAppWebhook.Services;

public class DefaultConversationFlow(IWhatsAppSender sender, AppDbContext db, IHubContext<ConversationHub> hub) : IConversationFlow
{
    public const string ButtonSobreMim = "menu_sobre_mim";
    public const string ButtonProjetos = "menu_projetos";
    public const string ButtonFalarComRafael = "menu_falar_com_rafael";
    private const string ProjectReplyPrefix = "projeto_";

    public async Task HandleAsync(Contact contact, WebhookMessage message, CancellationToken ct)
    {
        var replyId = message.Interactive?.ButtonReply?.Id ?? message.Interactive?.ListReply?.Id;
        var settings = await GetSettingsAsync(ct);

        switch (replyId)
        {
            case ButtonSobreMim:
                await sender.SendTextAsync(contact, settings.AboutMeText, ct);
                return;

            case ButtonProjetos:
                var projects = await db.Projects.OrderBy(p => p.DisplayOrder).ToListAsync(ct);
                await sender.SendListAsync(
                    contact,
                    "Aqui estão alguns dos meus projetos:",
                    "Ver projetos",
                    [.. projects.Select(p => ($"{ProjectReplyPrefix}{p.Id}", p.Title, (string?)p.ShortDescription))],
                    ct);
                return;

            case ButtonFalarComRafael:
                await sender.SendTextAsync(contact, settings.ContactConfirmationText, ct);
                contact.NeedsHumanAttention = true;
                await db.SaveChangesAsync(ct);
                await hub.Clients.All.SendAsync("humanRequested", new
                {
                    contactId = contact.Id,
                    waId = contact.WaId,
                    profileName = contact.ProfileName
                }, ct);
                return;
        }

        if (replyId is not null
            && replyId.StartsWith(ProjectReplyPrefix, StringComparison.Ordinal)
            && int.TryParse(replyId.AsSpan(ProjectReplyPrefix.Length), out var projectId))
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
            if (project is not null)
            {
                await sender.SendTextAsync(contact, project.DetailsText, ct);
                return;
            }
        }

        // Qualquer outra entrada (primeira mensagem, "menu"/"oi"/"olá", texto livre
        // não reconhecido) reapresenta o menu educadamente — seção 8.
        await sender.SendButtonsAsync(
            contact,
            settings.GreetingText,
            [
                (ButtonSobreMim, "Sobre mim"),
                (ButtonProjetos, "Projetos"),
                (ButtonFalarComRafael, "Falar com o Rafael")
            ],
            ct);
    }

    private async Task<BotSettings> GetSettingsAsync(CancellationToken ct)
    {
        return await db.BotSettings.FirstOrDefaultAsync(ct) ?? new BotSettings
        {
            GreetingText = "Olá! 👋 Sou o assistente do portfólio do Rafael. Como posso ajudar?",
            AboutMeText = "Muito Prazer! Sou desenvolvedor de software Full Stack, com experiência em sistemas baseados em .NET e Angular.",
            ContactConfirmationText = "Sua mensagem foi registrada! O Rafael irá entrar em contato com você em breve."
        };
    }
}
