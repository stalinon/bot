using System;
using CoreRouterMiddleware = Bot.Core.Middlewares.RouterMiddleware;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Hosting;
using Bot.Telegram;
#if (transport == "webhook")
using Microsoft.AspNetCore.Builder;
#endif
#if (admin)
using Bot.Admin.MinimalApi;
#endif
#if (webapp)
using Bot.WebApp.MinimalApi;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#if (transport == "webhook")
var builder = WebApplication.CreateBuilder(args);
#else
var builder = Host.CreateApplicationBuilder(args);
#endif

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

var cfg = builder.Configuration;

var services = builder.Services
    .AddBot(o =>
    {
        o.Token = cfg["BOT_TOKEN"] ?? throw new InvalidOperationException("BOT_TOKEN is required");
        cfg.GetSection("Transport").Bind(o.Transport);
    })
    .AddTelegramTransport()
    .AddWebApp(cfg)
    .UsePipeline(p => p
        .Use<ExceptionHandlingMiddleware>()
        .Use<LoggingMiddleware>()
        .Use<CoreRouterMiddleware>())
    .UseConfiguredStateStorage(cfg);

#if (admin)
services.AddAdminApi(cfg);
#endif

#if (transport == "webhook")
var app = builder.Build();
app.MapTelegramWebhook();
#if (admin)
app.MapAdminApi();
#endif
#if (webapp)
app.UseStrictCspForWebApp(cfg.GetSection("WebApp:Csp:AllowedOrigins").Get<string[]>() ?? []);
app.MapWebAppAuth();
app.MapWebAppMe();
#endif
await app.RunAsync();
#else
var host = builder.Build();
await host.RunAsync();
#endif
