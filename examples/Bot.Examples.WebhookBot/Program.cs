using Bot.Core.Options;
using Bot.Examples.WebhookBot.Services;
using Bot.Hosting;
using Bot.Telegram;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", false)
    .AddEnvironmentVariables();

var cfg = builder.Configuration;

builder.Services
    .AddBot(o =>
    {
        o.Token = cfg["BOT_TOKEN"] ?? throw new InvalidOperationException("BOT_TOKEN is required");
        cfg.GetSection("Transport").Bind(o.Transport);
        o.RateLimits = new RateLimitOptions { PerUserPerMinute = 20, PerChatPerMinute = 60, Mode = RateLimitMode.Soft };
    })
    .AddTelegramTransport()
    .AddScoped<RequestIdProvider>()
    .AddHandlersFromAssembly(typeof(Program).Assembly)
    .UsePipeline()
    .UseConfiguredStateStorage(cfg);

var app = builder.Build();

app.MapTelegramWebhook();

if (args.Contains("set-webhook", StringComparer.OrdinalIgnoreCase))
{
    await app.Services.GetRequiredService<WebhookService>().SetWebhookAsync(default);
    return;
}

if (args.Contains("delete-webhook", StringComparer.OrdinalIgnoreCase))
{
    await app.Services.GetRequiredService<WebhookService>().DeleteWebhookAsync(default);
    return;
}

await app.RunAsync();
