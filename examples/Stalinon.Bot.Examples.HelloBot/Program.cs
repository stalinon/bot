using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Scenes;
using Stalinon.Bot.Examples.HelloBot.Scenes;
using Stalinon.Bot.Examples.HelloBot.Services;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Telegram;

var builder = Host.CreateApplicationBuilder(args);

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
    .AddSingleton<ISceneNavigator>(sp =>
        new SceneNavigator(
            sp.GetRequiredService<IStateStore>(),
            TimeSpan.FromSeconds(cfg.GetValue("PHONE_STEP_TTL_SECONDS", 60))))
    .AddScoped<PhoneScene>(sp => new PhoneScene(sp.GetRequiredService<ITransportClient>(), sp.GetRequiredService<ISceneNavigator>(), TimeSpan.FromSeconds(cfg.GetValue("PHONE_STEP_TTL_SECONDS", 60))))
    .AddScoped<ProfileScene>(sp => new ProfileScene(sp.GetRequiredService<ISceneNavigator>(), sp.GetRequiredService<ITransportClient>(), TimeSpan.FromSeconds(cfg.GetValue("PHONE_STEP_TTL_SECONDS", 60))))
    .UsePipeline()
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
