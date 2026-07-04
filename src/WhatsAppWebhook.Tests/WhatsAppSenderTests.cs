using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Services;
using Xunit;

namespace WhatsAppWebhook.Tests;

public class FakeHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };
    }
}

public class WhatsAppSenderTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly Contact _contact;

    public WhatsAppSenderTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();

        _contact = new Contact { WaId = "5511999999999", CreatedAt = DateTimeOffset.UtcNow, LastSeenAt = DateTimeOffset.UtcNow };
        _db.Contacts.Add(_contact);
        _db.SaveChanges();
    }

    private static IConfiguration BuildConfiguration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["META_PHONE_NUMBER_ID"] = "1234567890",
            ["META_ACCESS_TOKEN"] = "token-de-teste",
            ["META_GRAPH_VERSION"] = "v21.0"
        })
        .Build();

    [Fact]
    public async Task SendTextAsync_ComRespostaDeSucesso_PersisteMensagemOutboundComWamId()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"messages":[{"id":"wamid.OUT123"}]}""");
        var httpClient = new HttpClient(handler);
        var sender = new WhatsAppSender(httpClient, _db, BuildConfiguration(), NullLogger<WhatsAppSender>.Instance);

        await sender.SendTextAsync(_contact, "Olá!");

        var saved = Assert.Single(_db.Messages.Local);
        Assert.Equal("wamid.OUT123", saved.WamId);
        Assert.Equal(MessageDirection.Outbound, saved.Direction);
        Assert.Equal("text", saved.Type);
        Assert.Equal("Olá!", saved.Body);

        using var sentBody = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("whatsapp", sentBody.RootElement.GetProperty("messaging_product").GetString());
        Assert.Equal(_contact.WaId, sentBody.RootElement.GetProperty("to").GetString());
        Assert.Equal("Olá!", sentBody.RootElement.GetProperty("text").GetProperty("body").GetString());
        Assert.Equal("Bearer token-de-teste", handler.LastRequest!.Headers.Authorization!.ToString());
        Assert.Contains("1234567890/messages", handler.LastRequest.RequestUri!.ToString());
    }

    [Fact]
    public async Task SendTextAsync_ComFalhaDaGraphApi_NaoLancaExcecaoENaoPersisteNada()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.Unauthorized, """{"error":"token inválido"}""");
        var httpClient = new HttpClient(handler);
        var sender = new WhatsAppSender(httpClient, _db, BuildConfiguration(), NullLogger<WhatsAppSender>.Instance);

        await sender.SendTextAsync(_contact, "Olá!");

        Assert.Empty(_db.Messages.Local);
    }

    [Fact]
    public async Task SendButtonsAsync_MontaPayloadInteractiveDeBotoes()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, """{"messages":[{"id":"wamid.OUT456"}]}""");
        var httpClient = new HttpClient(handler);
        var sender = new WhatsAppSender(httpClient, _db, BuildConfiguration(), NullLogger<WhatsAppSender>.Instance);

        await sender.SendButtonsAsync(_contact, "Como posso ajudar?", [("btn_a", "Opção A"), ("btn_b", "Opção B")]);

        using var sentBody = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("interactive", sentBody.RootElement.GetProperty("type").GetString());
        var buttons = sentBody.RootElement.GetProperty("interactive").GetProperty("action").GetProperty("buttons");
        Assert.Equal(2, buttons.GetArrayLength());
        Assert.Equal("btn_a", buttons[0].GetProperty("reply").GetProperty("id").GetString());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
