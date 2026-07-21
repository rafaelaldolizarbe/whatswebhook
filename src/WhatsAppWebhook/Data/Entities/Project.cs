namespace WhatsAppWebhook.Data.Entities;

public class Project
{
    public int Id { get; set; }

    public required string Title { get; set; }

    // Limite de 72 caracteres da Meta pra descrição de linha de lista interativa.
    public required string ShortDescription { get; set; }

    // Sem limite — usado na resposta de texto livre quando o usuário seleciona o item.
    public required string DetailsText { get; set; }

    public int DisplayOrder { get; set; }
}
