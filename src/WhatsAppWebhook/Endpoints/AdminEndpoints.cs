using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Dtos;
using WhatsAppWebhook.Services;

namespace WhatsAppWebhook.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/admin").RequireCors("AdminFrontend");

        admin.MapPost("/login", Login);
        admin.MapPost("/logout", Logout).RequireAuthorization();
        admin.MapGet("/session", () => Results.Ok()).RequireAuthorization();

        admin.MapGet("/contacts", GetContacts).RequireAuthorization();
        admin.MapGet("/contacts/{id:int}", GetContact).RequireAuthorization();
        admin.MapGet("/contacts/{id:int}/messages", GetContactMessages).RequireAuthorization();
        admin.MapPost("/contacts/{id:int}/messages", SendManualMessage).RequireAuthorization();
        admin.MapPost("/contacts/{id:int}/takeover", SetTakeover(true)).RequireAuthorization();
        admin.MapPost("/contacts/{id:int}/release", SetTakeover(false)).RequireAuthorization();
        admin.MapPost("/contacts/{id:int}/dismiss-attention", DismissAttention).RequireAuthorization();

        admin.MapGet("/projects", GetProjects).RequireAuthorization();
        admin.MapPost("/projects", CreateProject).RequireAuthorization();
        admin.MapPut("/projects/{id:int}", UpdateProject).RequireAuthorization();
        admin.MapDelete("/projects/{id:int}", DeleteProject).RequireAuthorization();

        admin.MapGet("/settings", GetSettings).RequireAuthorization();
        admin.MapPut("/settings", UpdateSettings).RequireAuthorization();
    }

    private static async Task<IResult> Login(LoginRequest request, SignInManager<IdentityUser> signInManager)
    {
        var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, isPersistent: true, lockoutOnFailure: true);
        return result.Succeeded ? Results.Ok() : Results.Unauthorized();
    }

    private static async Task<IResult> Logout(SignInManager<IdentityUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }

    private static async Task<IResult> GetContacts(AppDbContext db)
    {
        // SQLite/EF Core não traduz ORDER BY sobre DateTimeOffset para SQL —
        // ordena em memória; volume de contatos é pequeno o suficiente pra isso.
        var contacts = await db.Contacts.ToListAsync();
        var result = contacts
            .OrderByDescending(c => c.LastSeenAt)
            .Select(c => new ContactSummary(c.Id, c.WaId, c.ProfileName, c.LastSeenAt, c.HumanTakeoverEnabled, c.NeedsHumanAttention));

        return Results.Ok(result);
    }

    private static async Task<IResult> GetContact(int id, AppDbContext db)
    {
        var contact = await db.Contacts.FindAsync(id);
        return contact is null
            ? Results.NotFound()
            : Results.Ok(new ContactSummary(contact.Id, contact.WaId, contact.ProfileName, contact.LastSeenAt, contact.HumanTakeoverEnabled, contact.NeedsHumanAttention));
    }

    private static async Task<IResult> GetContactMessages(int id, AppDbContext db)
    {
        var messages = await db.Messages.Where(m => m.ContactId == id).ToListAsync();
        var result = messages
            .OrderBy(m => m.Timestamp)
            .Select(m => new MessageSummary(m.Id, m.Direction, m.Type, m.Body, m.Status, m.Timestamp));

        return Results.Ok(result);
    }

    private static async Task<IResult> SendManualMessage(int id, SendMessageRequest request, AppDbContext db, IWhatsAppSender sender, CancellationToken ct)
    {
        var contact = await db.Contacts.FindAsync([id], ct);
        if (contact is null)
        {
            return Results.NotFound();
        }

        await sender.SendTextAsync(contact, request.Text, ct);
        return Results.Ok();
    }

    private static Func<int, AppDbContext, CancellationToken, Task<IResult>> SetTakeover(bool enabled) =>
        async (id, db, ct) =>
        {
            var contact = await db.Contacts.FindAsync([id], ct);
            if (contact is null)
            {
                return Results.NotFound();
            }

            contact.HumanTakeoverEnabled = enabled;
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        };

    private static async Task<IResult> DismissAttention(int id, AppDbContext db, CancellationToken ct)
    {
        var contact = await db.Contacts.FindAsync([id], ct);
        if (contact is null)
        {
            return Results.NotFound();
        }

        contact.NeedsHumanAttention = false;
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> GetProjects(AppDbContext db)
    {
        var projects = await db.Projects.OrderBy(p => p.DisplayOrder).ToListAsync();
        return Results.Ok(projects);
    }

    private static async Task<IResult> CreateProject(ProjectRequest request, AppDbContext db, CancellationToken ct)
    {
        var project = new Project
        {
            Title = request.Title,
            ShortDescription = request.ShortDescription,
            DetailsText = request.DetailsText,
            DisplayOrder = request.DisplayOrder
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return Results.Ok(project);
    }

    private static async Task<IResult> UpdateProject(int id, ProjectRequest request, AppDbContext db, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([id], ct);
        if (project is null)
        {
            return Results.NotFound();
        }

        project.Title = request.Title;
        project.ShortDescription = request.ShortDescription;
        project.DetailsText = request.DetailsText;
        project.DisplayOrder = request.DisplayOrder;
        await db.SaveChangesAsync(ct);
        return Results.Ok(project);
    }

    private static async Task<IResult> DeleteProject(int id, AppDbContext db, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([id], ct);
        if (project is null)
        {
            return Results.NotFound();
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static async Task<IResult> GetSettings(AppDbContext db)
    {
        var settings = await db.BotSettings.FirstOrDefaultAsync();
        return settings is null ? Results.NotFound() : Results.Ok(settings);
    }

    private static async Task<IResult> UpdateSettings(BotSettingsRequest request, AppDbContext db, CancellationToken ct)
    {
        var settings = await db.BotSettings.FirstOrDefaultAsync(ct);
        if (settings is null)
        {
            return Results.NotFound();
        }

        settings.GreetingText = request.GreetingText;
        settings.AboutMeText = request.AboutMeText;
        settings.ContactConfirmationText = request.ContactConfirmationText;
        await db.SaveChangesAsync(ct);
        return Results.Ok(settings);
    }
}

public record LoginRequest(string Email, string Password);

public record ContactSummary(int Id, string WaId, string? ProfileName, DateTimeOffset LastSeenAt, bool HumanTakeoverEnabled, bool NeedsHumanAttention);

public record SendMessageRequest(string Text);

public record ProjectRequest(string Title, string ShortDescription, string DetailsText, int DisplayOrder);

public record BotSettingsRequest(string GreetingText, string AboutMeText, string ContactConfirmationText);
