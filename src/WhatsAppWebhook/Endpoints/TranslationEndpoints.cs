using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;

namespace WhatsAppWebhook.Endpoints;

// API pública consumida pelo site Next.js (rfchdev.com.br) — autenticação por
// X-Api-Key (não pelo cookie do painel administrativo), rate limit e CORS
// próprios, separados do resto da app. Ver translations-api-backend.md.
public static class TranslationEndpoints
{
    public static void MapTranslationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/translations")
            .RequireRateLimiting("translations")
            .RequireCors("TranslationsFrontend")
            .AddEndpointFilter(RequireApiKey);

        group.MapGet("/{locale}", GetByLocale);
        group.MapGet("/", GetSupportedLocales);
    }

    private static async ValueTask<object?> RequireApiKey(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expectedKey = configuration["TRANSLATIONS_API_KEY"];
        var providedKey = context.HttpContext.Request.Headers["X-Api-Key"].ToString();

        if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }

    private static async Task<IResult> GetByLocale(string locale, AppDbContext db)
    {
        var resources = await db.TranslationResources
            .Where(r => r.Locale == locale.ToLower())
            .ToListAsync();

        if (resources.Count == 0)
        {
            return Results.NotFound(new { error = $"No translations found for locale '{locale}'." });
        }

        var merged = new Dictionary<string, JsonElement>();
        foreach (var resource in resources)
        {
            merged[resource.Namespace] = JsonSerializer.Deserialize<JsonElement>(resource.ContentJson);
        }

        return Results.Ok(merged);
    }

    private static async Task<IResult> GetSupportedLocales(AppDbContext db)
    {
        var locales = await db.TranslationResources.Select(r => r.Locale).Distinct().ToListAsync();
        return Results.Ok(new { locales });
    }
}
