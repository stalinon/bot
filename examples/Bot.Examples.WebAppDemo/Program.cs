using Bot.Abstractions.Contracts;
using Bot.Hosting.Options;
using Bot.Telegram;
using Bot.WebApp.MinimalApi;

namespace Bot.Examples.WebAppDemo;

/// <summary>
///     Точка входа хоста Mini App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Настраивает HTTP-сервер.</item>
///         <item>Добавляет middleware безопасности.</item>
///         <item>Отдаёт статические файлы и эндпоинт авторизации.</item>
///     </list>
/// </remarks>
public static class Program
{
    /// <summary>
    ///     Запустить приложение.
    /// </summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<BotOptions>(builder.Configuration.GetSection("Bot"));
        builder.Services.Configure<WebAppAuthOptions>(builder.Configuration.GetSection("WebAppAuth"));
        builder.Services.AddSingleton<IWebAppInitDataValidator, WebAppInitDataValidator>();

        var app = builder.Build();

        app.UseStrictCspForWebApp(
            builder.Configuration.GetSection("WebAppCsp:AllowedOrigins").Get<string[]>() ?? []);

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.MapWebAppAuth();

        app.Run();
    }
}
