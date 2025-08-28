using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Hosting;
using Bot.Hosting.Options;
using Bot.Storage.File;
using Bot.Storage.File.Options;
using Bot.Telegram;
using Microsoft.Extensions.Configuration;
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
        o.Transport = TransportMode.Polling;
        o.Parallelism = 8;
        o.RateLimits = new RateLimitOptions { PerUserPerMinute = 20, PerChatPerMinute = 60 };
    })
    .AddTelegramTransport()
    .AddHandlersFromAssembly(typeof(Program).Assembly)
    .UsePipeline(p => p
        .Use<ExceptionHandlingMiddleware>()
        .Use<LoggingMiddleware>()
        .Use<DedupMiddleware>()
        .Use<RateLimitMiddleware>()
        .Use<CommandParsingMiddleware>()
        .Use<RouterMiddleware>())
    .UseStateStore(new FileStateStore(new FileStoreOptions { Path = cfg["DATA_PATH"] ?? "data" }));

await builder.Build().RunAsync();