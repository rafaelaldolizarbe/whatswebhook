using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Models;

namespace WhatsAppWebhook.Services;

public class DefaultConversationFlow(IWhatsAppSender sender) : IConversationFlow
{
    public const string ButtonSobreMim = "menu_sobre_mim";
    public const string ButtonProjetos = "menu_projetos";
    public const string ButtonFalarComRafael = "menu_falar_com_rafael";

    // Fonte única dos projetos: a lista (limite de 72 caracteres na descrição,
    // regra da Meta) e o texto detalhado (sem limite, cabem os links) vêm daqui,
    // pra não duplicar o catálogo em dois lugares.
    private static readonly Project[] Projects =
    [
        new Project(
            "projeto_1",
            "Perfil Corporativo",
            "Página institucional com minhas informações e competências.",
            "Perfil Corporativo: página institucional com minhas informações e competências.\n" +
            "Repositório: https://github.com/rafaelaldolizarbe/profile_dev\n" +
            "Site: https://www.rfchdev.com.br/pt"),
        new Project(
            "projeto_2",
            "AgendaAI",
            "Agendamento de serviços de beleza, integrado ao WhatsApp.",
            "AgendaAI: sistema de agendamento de serviços de beleza, integrado ao WhatsApp (em desenvolvimento).\n" +
            "Repositório: https://github.com/rafaelaldolizarbe/agendai-web"),
        new Project(
            "projeto_3",
            "EasyFind",
            "Buscador de produtos com integração de geolocalização.",
            "EasyFind: buscador de produtos com integração de geolocalização.\n" +
            "Repositório: https://github.com/Easy-Find")
    ];

    public async Task HandleAsync(Contact contact, WebhookMessage message, CancellationToken ct)
    {
        var replyId = message.Interactive?.ButtonReply?.Id ?? message.Interactive?.ListReply?.Id;

        switch (replyId)
        {
            case ButtonSobreMim:
                await sender.SendTextAsync(contact, "Muito Prazer! Sou desenvolvedor de software Full Stack, com experiênci em sistemas baseados em .NET e Angular. Atuo na criação de soluções inovadoras e eficientes, sempre buscando a excelência técnica e a melhor experiência para os usuários.", ct);
                return;

            case ButtonProjetos:
                await sender.SendListAsync(
                    contact,
                    "Aqui estão alguns dos meus projetos:",
                    "Ver projetos",
                    [.. Projects.Select(p => (p.Id, p.Title, (string?)p.ShortDescription))],
                    ct);
                return;

            case ButtonFalarComRafael:
                await sender.SendTextAsync(contact, "Sua mensagem foi registrada! O Rafael irá entrar em contato com você em breve.", ct);
                return;
        }

        var selectedProject = Projects.FirstOrDefault(p => p.Id == replyId);
        if (selectedProject.Id is not null)
        {
            await sender.SendTextAsync(contact, selectedProject.DetailsText, ct);
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

    private readonly record struct Project(string Id, string Title, string ShortDescription, string DetailsText);
}
