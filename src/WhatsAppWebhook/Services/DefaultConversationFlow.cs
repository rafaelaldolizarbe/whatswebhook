using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Models;

namespace WhatsAppWebhook.Services;

public class DefaultConversationFlow(IWhatsAppSender sender) : IConversationFlow
{
    public const string ButtonSobreMim = "menu_sobre_mim";
    public const string ButtonProjetos = "menu_projetos";
    public const string ButtonFalarComRafael = "menu_falar_com_rafael";

    public async Task HandleAsync(Contact contact, WebhookMessage message, CancellationToken ct)
    {
        var replyId = message.Interactive?.ButtonReply?.Id ?? message.Interactive?.ListReply?.Id;

        switch (replyId)
        {
            case ButtonSobreMim:
                // TODO: preencher com o texto de apresentação definitivo do Rafael.
                await sender.SendTextAsync(contact, "TODO: texto curto de apresentação do Rafael.", ct);
                return;

            case ButtonProjetos:
                await sender.SendListAsync(
                    contact,
                    "Aqui estão alguns dos meus projetos:",
                    "Ver projetos",
                    [
                        // TODO: substituir pelos projetos reais do portfólio.
                        ("projeto_1", "Projeto 1 (TODO)", "TODO: descrição curta"),
                        ("projeto_2", "Projeto 2 (TODO)", "TODO: descrição curta"),
                        ("projeto_3", "Projeto 3 (TODO)", "TODO: descrição curta")
                    ],
                    ct);
                return;

            case ButtonFalarComRafael:
                await sender.SendTextAsync(contact, "Sua mensagem foi registrada! O Rafael vai te responder por aqui em breve. 🙌", ct);
                return;
        }

        // Qualquer outra entrada (primeira mensagem, "menu"/"oi"/"olá", texto livre
        // não reconhecido) reapresenta o menu educadamente — seção 8.
        await sender.SendButtonsAsync(
            contact,
            "Olá! 👋 Sou o assistente do portfólio do Rafael. Como posso ajudar?",
            [
                (ButtonSobreMim, "Sobre mim"),
                (ButtonProjetos, "Projetos"),
                (ButtonFalarComRafael, "Falar com o Rafael")
            ],
            ct);
    }
}
