using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;
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
    public IReadOnlyList<(string Id, string Title, string? Description)>? LastListRows { get; private set; }

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
        LastListRows = rows;
        return Task.CompletedTask;
    }
}

public class DefaultConversationFlowTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    // Instância por teste (não static) — o caso "Falar com o Rafael" agora
    // muda o próprio objeto (NeedsHumanAttention), então não pode ser compartilhado.
    private readonly Contact _contact = new()
    {
        Id = 1,
        WaId = "5511999999999",
        CreatedAt = DateTimeOffset.UtcNow,
        LastSeenAt = DateTimeOffset.UtcNow
    };

    public DefaultConversationFlowTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task HandleAsync_ComTextoLivre_ReapresentaOMenu()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender, _db, new NoOpHubContext());

        await flow.HandleAsync(_contact, new WebhookMessage { Type = "text", Text = new WebhookText { Body = "qualquer coisa" } }, CancellationToken.None);

        Assert.Equal(1, sender.ButtonsSentCount);
    }

    [Fact]
    public async Task HandleAsync_ComSaudacao_MostraOMenu()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender, _db, new NoOpHubContext());

        await flow.HandleAsync(_contact, new WebhookMessage { Type = "text", Text = new WebhookText { Body = "oi" } }, CancellationToken.None);

        Assert.Equal(1, sender.ButtonsSentCount);
    }

    [Fact]
    public async Task HandleAsync_ComBotaoSobreMim_EnviaTextoDeApresentacao()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender, _db, new NoOpHubContext());
        var message = new WebhookMessage
        {
            Type = "interactive",
            Interactive = new WebhookInteractive { ButtonReply = new WebhookInteractiveReply { Id = DefaultConversationFlow.ButtonSobreMim } }
        };

        await flow.HandleAsync(_contact, message, CancellationToken.None);

        Assert.Single(sender.TextsSent);
        Assert.Equal(0, sender.ButtonsSentCount);
    }

    [Fact]
    public async Task HandleAsync_ComBotaoProjetos_EnviaListaDoBanco()
    {
        _db.Projects.Add(new Project { Title = "Projeto Teste", ShortDescription = "Descrição curta", DetailsText = "Detalhe completo", DisplayOrder = 1 });
        await _db.SaveChangesAsync();

        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender, _db, new NoOpHubContext());
        var message = new WebhookMessage
        {
            Type = "interactive",
            Interactive = new WebhookInteractive { ButtonReply = new WebhookInteractiveReply { Id = DefaultConversationFlow.ButtonProjetos } }
        };

        await flow.HandleAsync(_contact, message, CancellationToken.None);

        Assert.Equal(1, sender.ListsSentCount);
        Assert.Single(sender.LastListRows!);
        Assert.Equal("Projeto Teste", sender.LastListRows![0].Title);
    }

    [Fact]
    public async Task HandleAsync_ComSelecaoDeProjeto_EnviaTextoDetalhado()
    {
        var project = new Project { Title = "Projeto Teste", ShortDescription = "Curta", DetailsText = "Texto detalhado com link" };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender, _db, new NoOpHubContext());
        var message = new WebhookMessage
        {
            Type = "interactive",
            Interactive = new WebhookInteractive { ListReply = new WebhookInteractiveReply { Id = $"projeto_{project.Id}" } }
        };

        await flow.HandleAsync(_contact, message, CancellationToken.None);

        Assert.Equal("Texto detalhado com link", Assert.Single(sender.TextsSent));
    }

    [Fact]
    public async Task HandleAsync_ComBotaoFalarComRafael_ConfirmaRegistro()
    {
        var sender = new FakeWhatsAppSender();
        var flow = new DefaultConversationFlow(sender, _db, new NoOpHubContext());
        var message = new WebhookMessage
        {
            Type = "interactive",
            Interactive = new WebhookInteractive { ButtonReply = new WebhookInteractiveReply { Id = DefaultConversationFlow.ButtonFalarComRafael } }
        };

        await flow.HandleAsync(_contact, message, CancellationToken.None);

        Assert.Single(sender.TextsSent);
        Assert.True(_contact.NeedsHumanAttention);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
