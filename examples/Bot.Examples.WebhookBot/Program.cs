using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Hosting;
using Bot.Hosting.Options;
using Bot.Telegram;
using Bot.Examples.WebhookBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

var cfg = builder.Configuration;

builder.Services
    .AddBot(o =>
    {
        o.Token = cfg["BOT_TOKEN"] ?? throw new InvalidOperationException("BOT_TOKEN is required");
        o.Transport = new TransportOptions
        {
            Mode = TransportMode.Webhook,
            PublicUrl = cfg["PUBLIC_URL"] ?? throw new InvalidOperationException("PUBLIC_URL is required"),
            Secret = cfg["WEBHOOK_SECRET"] ?? "secret"
        };
        o.Parallelism = 8;
        o.RateLimits = new RateLimitOptions { PerUserPerMinute = 20, PerChatPerMinute = 60, Mode = RateLimitMode.Soft };
    })
    .AddTelegramTransport()
    .AddScoped<RequestIdProvider>()
    .AddHandlersFromAssembly(typeof(Program).Assembly)
    .UsePipeline(p => p
        .Use<ExceptionHandlingMiddleware>()
        .Use<MetricsMiddleware>()
        .Use<LoggingMiddleware>()
        .Use<DedupMiddleware>()
        .Use<RateLimitMiddleware>()
        .Use<CommandParsingMiddleware>()
        .Use<Bot.Core.Middlewares.RouterMiddleware>())
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
