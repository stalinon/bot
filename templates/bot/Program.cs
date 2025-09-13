using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
#if (admin)
using Stalinon.Bot.Admin.MinimalApi;
#endif
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Telegram;
#if (webapp)
using Stalinon.Bot.WebApp.MinimalApi;
#endif

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("Configuration/appsettings.json", false)
    .AddEnvironmentVariables();

var cfg = builder.Configuration;

builder.Services
    .AddBot(o =>
    {
        o.Token = cfg["Bot:Token"] ?? throw new InvalidOperationException("BOT_TOKEN is required");
        cfg.GetSection("Bot:Transport").Bind(o.Transport);
    })
    .AddTelegramTransport()
    #if (webapp)
    .AddWebApp(cfg)
    #endif
    #if (admin)
    .AddAdminApi(cfg)
    #endif
    .AddHandlersFromAssembly(typeof(Program).Assembly)
    .UsePipeline()
    .UseConfiguredStateStorage(cfg);

var app = builder.Build();

#if (transport == "webhook")
app.MapTelegramWebhook();
#endif
#if (webapp)
app.MapWebAppAuth();
app.MapWebAppMe();
#endif
#if (admin)
app.MapHealth();
app.MapAdminStats();
#endif

app.Run();
