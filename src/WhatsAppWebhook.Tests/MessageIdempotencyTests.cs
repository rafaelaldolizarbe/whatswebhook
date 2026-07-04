using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Data.Entities;
using Xunit;

namespace WhatsAppWebhook.Tests;

public class MessageIdempotencyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;

    public MessageIdempotencyTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveChanges_ComWamIdDuplicado_LancaExcecaoDeUnicidade()
    {
        var contact = new Contact
        {
            WaId = "5511999999999",
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync();

        _db.Messages.Add(new Message
        {
            WamId = "wamid.DUPLICADO",
            ContactId = contact.Id,
            Direction = MessageDirection.Inbound,
            Type = "text",
            Body = "primeira mensagem",
            Timestamp = DateTimeOffset.UtcNow,
            RawPayload = "{}"
        });
        await _db.SaveChangesAsync();

        _db.Messages.Add(new Message
        {
            WamId = "wamid.DUPLICADO",
            ContactId = contact.Id,
            Direction = MessageDirection.Inbound,
            Type = "text",
            Body = "reenvio da mesma mensagem",
            Timestamp = DateTimeOffset.UtcNow,
            RawPayload = "{}"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());

        Assert.Equal(1, await _db.Messages.CountAsync(m => m.WamId == "wamid.DUPLICADO"));
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
