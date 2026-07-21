using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data.Entities;

namespace WhatsAppWebhook.Data;

// IdentityDbContext traz as tabelas de usuário/login do painel administrativo
// (autenticação por cookie, usuário único semeado no startup — ver Program.cs).
public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<BotSettings> BotSettings => Set<BotSettings>();

    public DbSet<TranslationResource> TranslationResources => Set<TranslationResource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contact>()
            .HasIndex(c => c.WaId)
            .IsUnique();

        modelBuilder.Entity<TranslationResource>()
            .HasIndex(t => new { t.Locale, t.Namespace })
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
