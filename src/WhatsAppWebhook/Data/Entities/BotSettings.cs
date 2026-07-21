namespace WhatsAppWebhook.Data.Entities;

// Linha única (Id sempre 1) com os textos editáveis do menu — ver seed em
// Program.cs e a checagem em DefaultConversationFlow.
public class BotSettings
{
    public int Id { get; set; }

    public required string GreetingText { get; set; }

    public required string AboutMeText { get; set; }

    public required string ContactConfirmationText { get; set; }
}
