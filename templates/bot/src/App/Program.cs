using Stalinon.Bot.Admin.MinimalApi;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Storage.File;
using Stalinon.Bot.Transport.Telegram;
using Stalinon.Bot.WebApp.MinimalApi;

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;

builder.Services
    .AddBot(o => o.Token = cfg["BOT_TOKEN"] ?? "token")
    .AddTelegramPolling()
    .AddFileStore()
    .AddHandlersFromAssembly(typeof(Program).Assembly)
    .UsePipeline();

var app = builder.Build();

{{if webapp}}
app.MapWebAppAuth();
app.MapWebAppMe();
{{endif}}
{{if admin}}
app.MapHealth();
app.MapAdminStats();
{{endif}}

app.Run();
