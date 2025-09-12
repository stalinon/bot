using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Stalinon.Bot.Admin.MinimalApi;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Telegram;
using Stalinon.Bot.WebApp.MinimalApi;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddJsonFile("Configuration/appsettings.json", false)
    .AddEnvironmentVariables();

var cfg = builder.Configuration;

builder.Services
    .AddBot(o =>
    {
        o.Token = cfg["BOT_TOKEN"] ?? "token";
        cfg.GetSection("Transport").Bind(o.Transport);
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
