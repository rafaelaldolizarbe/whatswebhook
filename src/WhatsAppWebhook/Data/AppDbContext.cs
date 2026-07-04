using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data.Entities;

namespace WhatsAppWebhook.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.WaId)
            .IsUnique();

        modelBuilder.Entity<Message>()
            .HasIndex(m => m.WamId)
            .IsUnique();

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Contact)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ContactId);
    }
}
