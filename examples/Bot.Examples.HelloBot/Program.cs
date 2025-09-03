using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Hosting;
using Bot.Hosting.Options;
using Bot.Telegram;
using Bot.Examples.HelloBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

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
            Mode = TransportMode.Polling
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
