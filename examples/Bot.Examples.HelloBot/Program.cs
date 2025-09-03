using System;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Core.Scenes;
using Bot.Examples.HelloBot.Services;
using Bot.Examples.HelloBot.Scenes;
using Bot.Hosting;
using Bot.Telegram;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
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
    .AddSingleton<ISceneNavigator>(sp =>
        new SceneNavigator(
            sp.GetRequiredService<IStateStore>(),
            TimeSpan.FromSeconds(cfg.GetValue("PHONE_STEP_TTL_SECONDS", 60))))
    .AddScoped<PhoneScene>()
    .UsePipeline(p => p
        .Use<ExceptionHandlingMiddleware>()
        .Use<MetricsMiddleware>()
        .Use<LoggingMiddleware>()
        .Use<DedupMiddleware>()
        .Use<RateLimitMiddleware>()
        .Use<CommandParsingMiddleware>()
        .Use<RouterMiddleware>())
    .UseConfiguredStateStorage(cfg);

var host = builder.Build();

if (args.Contains("set-webhook", StringComparer.OrdinalIgnoreCase))
{
    await host.Services.GetRequiredService<WebhookService>().SetWebhookAsync(default);
    return;
}

if (args.Contains("delete-webhook", StringComparer.OrdinalIgnoreCase))
{
    await host.Services.GetRequiredService<WebhookService>().DeleteWebhookAsync(default);
    return;
}

await host.RunAsync();
