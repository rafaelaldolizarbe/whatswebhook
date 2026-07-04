using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using WhatsAppWebhook.Data;
using WhatsAppWebhook.Endpoints;
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapWebhookEndpoints();

app.Run();
