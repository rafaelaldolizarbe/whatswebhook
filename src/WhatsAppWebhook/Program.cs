using System.Threading.Channels;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Data.Entities;
using WhatsAppWebhook.Endpoints;
using WhatsAppWebhook.Hubs;
using WhatsAppWebhook.Services;

var builder = WebApplication.CreateBuilder(args);

var dbPath = builder.Configuration["DB_PATH"] ?? "./data/app.db";
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbPath))!);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddSingleton(Channel.CreateUnbounded<string>());
builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<string>>().Reader);
builder.Services.AddSingleton(sp => sp.GetRequiredService<Channel<string>>().Writer);
builder.Services.AddHostedService<MessageProcessor>();

builder.Services.AddHttpClient<IWhatsAppSender, WhatsAppSender>();
builder.Services.AddScoped<IConversationFlow, DefaultConversationFlow>();

// Painel administrativo conecta em /hubs/conversation pra ver mensagens
// chegando/saindo em tempo real, sem precisar recarregar a página.
builder.Services.AddSignalR();

// Autenticação do painel administrativo (whatsapp-admin-client), usuário único
// semeado a partir do .env — sem tela de cadastro (ver seed logo abaixo).
builder.Services.AddIdentityCore<IdentityUser>(options =>
    {
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme, options =>
    {
        options.Cookie.Name = "wa_admin_auth";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        var cookieDomain = builder.Configuration["ADMIN_COOKIE_DOMAIN"];
        if (!string.IsNullOrEmpty(cookieDomain))
        {
            options.Cookie.Domain = cookieDomain;
        }
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// whatsapp-admin-client roda num subdomínio separado (admin.<dominio>) —
// precisa de CORS explícito + credentials pro cookie de sessão ser aceito.
// Aceita lista separada por vírgula (ex.: produção + ambiente local de teste).
static string[] SplitOrigins(string? value) =>
    string.IsNullOrEmpty(value)
        ? []
        : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

var adminFrontendOrigins = SplitOrigins(builder.Configuration["ADMIN_FRONTEND_ORIGIN"]);
var translationsAllowedOrigins = SplitOrigins(builder.Configuration["TRANSLATIONS_ALLOWED_ORIGIN"]);
builder.Services.AddCors(options =>
{
    options.AddPolicy("AdminFrontend", policy =>
    {
        if (adminFrontendOrigins.Length > 0)
        {
            policy.WithOrigins(adminFrontendOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });

    // API pública de traduções (X-Api-Key, não usa cookie) — origem do site
    // Next.js, sempre diferente da origem do painel administrativo.
    options.AddPolicy("TranslationsFrontend", policy =>
    {
        if (translationsAllowedOrigins.Length > 0)
        {
            policy.WithOrigins(translationsAllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

// Rate limiting da API pública de traduções — sliding window 60 req/min/IP,
// mesma config do documento original (translations-api-backend.md).
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("translations", limiter =>
    {
        limiter.PermitLimit = 60;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.SegmentsPerWindow = 6;
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit = 5;
    });

    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests.", ct);
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var adminEmail = builder.Configuration["ADMIN_EMAIL"];
    var adminPassword = builder.Configuration["ADMIN_PASSWORD"];
    if (!string.IsNullOrEmpty(adminEmail) && !string.IsNullOrEmpty(adminPassword))
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
            await userManager.CreateAsync(admin, adminPassword);
        }
    }

    // Semeia os textos/projetos que antes eram fixos no código (DefaultConversationFlow),
    // só na primeira vez — depois disso o conteúdo é editado pelo painel administrativo.
    if (!await db.BotSettings.AnyAsync())
    {
        db.BotSettings.Add(new BotSettings
        {
            GreetingText = "Olá! 👋 Sou o assistente do portfólio do Rafael. Como posso ajudar?",
            AboutMeText = "Muito Prazer! Sou desenvolvedor de software Full Stack, com experiênci em sistemas baseados em .NET e Angular. Atuo na criação de soluções inovadoras e eficientes, sempre buscando a excelência técnica e a melhor experiência para os usuários.",
            ContactConfirmationText = "Sua mensagem foi registrada! O Rafael irá entrar em contato com você em breve."
        });
    }

    if (!await db.Projects.AnyAsync())
    {
        db.Projects.AddRange(
            new Project
            {
                Title = "Perfil Corporativo",
                ShortDescription = "Página institucional com minhas informações e competências.",
                DetailsText = "Perfil Corporativo: página institucional com minhas informações e competências.\n" +
                    "Repositório: https://github.com/rafaelaldolizarbe/profile_dev\n" +
                    "Site: https://www.rfchdev.com.br/pt",
                DisplayOrder = 1
            },
            new Project
            {
                Title = "AgendaAI",
                ShortDescription = "Agendamento de serviços de beleza, integrado ao WhatsApp.",
                DetailsText = "AgendaAI: sistema de agendamento de serviços de beleza, integrado ao WhatsApp (em desenvolvimento).\n" +
                    "Repositório: https://github.com/rafaelaldolizarbe/agendai-web",
                DisplayOrder = 2
            },
            new Project
            {
                Title = "EasyFind",
                ShortDescription = "Buscador de produtos com integração de geolocalização.",
                DetailsText = "EasyFind: buscador de produtos com integração de geolocalização.\n" +
                    "Repositório: https://github.com/Easy-Find",
                DisplayOrder = 3
            });
    }

    await db.SaveChangesAsync();
}

app.UseCors("AdminFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapWebhookEndpoints();
app.MapAdminEndpoints();
app.MapTranslationsAdminEndpoints();
app.MapTranslationEndpoints();
app.MapHub<ConversationHub>("/hubs/conversation").RequireCors("AdminFrontend");

app.Run();
