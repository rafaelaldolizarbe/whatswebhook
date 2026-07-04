using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Models;
using WhatsAppWebhook.Services;
using Xunit;

namespace WhatsAppWebhook.Tests;

public class FakeWhatsAppSender : IWhatsAppSender
{
    public List<string> TextsSent { get; } = [];
    public int ButtonsSentCount { get; private set; }
    public int ListsSentCount { get; private set; }

    public Task SendTextAsync(Contact contact, string body, CancellationToken ct = default)
    {
        TextsSent.Add(body);
        return Task.CompletedTask;
    }

    public Task SendButtonsAsync(Contact contact, string bodyText, IReadOnlyList<(string Id, string Title)> buttons, CancellationToken ct = default)
    {
        ButtonsSentCount++;
        return Task.CompletedTask;
    }

    public Task SendListAsync(Contact contact, string bodyText, string buttonLabel, IReadOnlyList<(string Id, string Title, string? Description)> rows, CancellationToken ct = default)
    {
        ListsSentCount++;
        return Task.CompletedTask;
    }
}

public class DefaultConversationFlowTests
{
    private static readonly Contact Contact = new()
    {
        Id = 1,
        WaId = "5511999999999",
        CreatedAt = DateTimeOffset.UtcNow,
        LastSeenAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task HandleAsync_ComTextoLivre_ReapresentaOMenu()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender);

        await flow.HandleAsync(Contact, new WebhookMessage { Type = "text", Text = new WebhookText { Body = "qualquer coisa" } }, CancellationToken.None);

        Assert.Equal(1, sender.ButtonsSentCount);
    }

    [Fact]
    public async Task HandleAsync_ComSaudacao_MostraOMenu()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender);

        await flow.HandleAsync(Contact, new WebhookMessage { Type = "text", Text = new WebhookText { Body = "oi" } }, CancellationToken.None);

        Assert.Equal(1, sender.ButtonsSentCount);
    }

    [Fact]
    public async Task HandleAsync_ComBotaoSobreMim_EnviaTextoDeApresentacao()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender);
        var message = new WebhookMessage
        {
            Type = "interactive",
            Interactive = new WebhookInteractive { ButtonReply = new WebhookInteractiveReply { Id = DefaultConversationFlow.ButtonSobreMim } }
        };

        await flow.HandleAsync(Contact, message, CancellationToken.None);

        Assert.Single(sender.TextsSent);
        Assert.Equal(0, sender.ButtonsSentCount);
    }

    [Fact]
    public async Task HandleAsync_ComBotaoProjetos_EnviaLista()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender);
        var message = new WebhookMessage
        {
            Type = "interactive",
            Interactive = new WebhookInteractive { ButtonReply = new WebhookInteractiveReply { Id = DefaultConversationFlow.ButtonProjetos } }
        };

        await flow.HandleAsync(Contact, message, CancellationToken.None);

        Assert.Equal(1, sender.ListsSentCount);
    }

    [Fact]
    public async Task HandleAsync_ComBotaoFalarComRafael_ConfirmaRegistro()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender);
        var message = new WebhookMessage
        {
            Type = "interactive",
            Interactive = new WebhookInteractive { ButtonReply = new WebhookInteractiveReply { Id = DefaultConversationFlow.ButtonFalarComRafael } }
        };

        await flow.HandleAsync(Contact, message, CancellationToken.None);

        Assert.Single(sender.TextsSent);
    }
}
