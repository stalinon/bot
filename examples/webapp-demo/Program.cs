using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace Examples.WebAppDemo;

/// <summary>
///	Точка входа хоста Mini App.
/// </summary>
/// <remarks>
///	<list type="number">
///		<item>Настраивает HTTP-сервер.</item>
///		<item>Добавляет middleware безопасности.</item>
///		<item>Отдаёт статические файлы.</item>
///	</list>
/// </remarks>
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append(
                "Content-Security-Policy",
                "default-src 'self' https://telegram.org https://*.telegram.org;");
            context.Response.Headers.Append("Referrer-Policy", "no-referrer");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            await next();
        });

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.Run();
    }
}
