using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.WebApp.MinimalApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<WebAppStatsCollector>();
var app = builder.Build();
app.MapWebAppAuth();
app.MapWebAppMe();
app.Run();

namespace Stalinon.Bot.WebApp.MinimalApi.Tests
{
    /// <summary>
    ///     Точка входа тестового приложения Web App API.
    /// </summary>
    public class Program
    {
    }
}
