using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace WhatsAppWebhook.Hubs;

// O painel administrativo conecta aqui pra receber mensagens/status em tempo
// real de uma conversa específica, sem precisar dar F5. MessageProcessor e
// WhatsAppSender usam IHubContext<ConversationHub> pra avisar este hub sempre
// que uma mensagem inbound/outbound é persistida.
[Authorize]
public class ConversationHub : Hub
{
    public Task JoinContact(int contactId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(contactId));

    public Task LeaveContact(int contactId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(contactId));

    public static string GroupName(int contactId) => $"contact-{contactId}";
}
