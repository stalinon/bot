using Bot.WebApp;
using Bot.WebApp.MinimalApi;

var builder = WebApplication.CreateBuilder(args);

// Минимальная конфигурация Mini App
builder.Services.AddMiniApp();

var app = builder.Build();

app.MapWebAppAuth();
app.MapWebAppMe();

app.Run();
