using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Data.Entities;

namespace WhatsAppWebhook.Endpoints;

public static class TranslationsAdminEndpoints
{
    public static void MapTranslationsAdminEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/admin/translations").RequireCors("AdminFrontend").RequireAuthorization();

        admin.MapGet("/", GetAll);
        admin.MapPost("/", Create);
        admin.MapPut("/{id:int}", Update);
        admin.MapDelete("/{id:int}", Delete);
    }

    private static async Task<IResult> GetAll(AppDbContext db)
    {
        var resources = await db.TranslationResources
            .OrderBy(r => r.Locale)
            .ThenBy(r => r.Namespace)
            .ToListAsync();

        return Results.Ok(resources);
    }

    private static async Task<IResult> Create(TranslationResourceRequest request, AppDbContext db, CancellationToken ct)
    {
        if (!IsValidJson(request.ContentJson))
        {
            return Results.BadRequest(new { error = "contentJson não é um JSON válido." });
        }

        var now = DateTimeOffset.UtcNow;
        var resource = new TranslationResource
        {
            Locale = request.Locale,
            Namespace = request.Namespace,
            ContentJson = request.ContentJson,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.TranslationResources.Add(resource);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { error = $"Já existe um registro para locale '{request.Locale}' + namespace '{request.Namespace}'." });
        }

        return Results.Ok(resource);
    }

    private static async Task<IResult> Update(int id, TranslationResourceRequest request, AppDbContext db, CancellationToken ct)
    {
        if (!IsValidJson(request.ContentJson))
        {
            return Results.BadRequest(new { error = "contentJson não é um JSON válido." });
        }

        var resource = await db.TranslationResources.FindAsync([id], ct);
        if (resource is null)
        {
            return Results.NotFound();
        }

        resource.Locale = request.Locale;
        resource.Namespace = request.Namespace;
        resource.ContentJson = request.ContentJson;
        resource.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(resource);
    }

    private static async Task<IResult> Delete(int id, AppDbContext db, CancellationToken ct)
    {
        var resource = await db.TranslationResources.FindAsync([id], ct);
        if (resource is null)
        {
            return Results.NotFound();
        }

        db.TranslationResources.Remove(resource);
        await db.SaveChangesAsync(ct);
        return Results.Ok();
    }

    private static bool IsValidJson(string content)
    {
        try
        {
            JsonDocument.Parse(content);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public record TranslationResourceRequest(string Locale, string Namespace, string ContentJson);
