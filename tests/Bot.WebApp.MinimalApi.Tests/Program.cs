using Bot.WebApp.MinimalApi;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapWebAppAuth();
app.MapWebAppMe();
app.Run();

namespace Bot.WebApp.MinimalApi.Tests
{
    /// <summary>
    ///     Точка входа тестового приложения Web App API.
    /// </summary>
    public partial class Program
    {
    }
}
