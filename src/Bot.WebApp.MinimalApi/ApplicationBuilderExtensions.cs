namespace Bot.WebApp.MinimalApi;

/// <summary>
///     Расширения для приложения Mini App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Добавляет строгие заголовки CSP.</item>
///     </list>
/// </remarks>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    ///     Подключить строгую политику безопасности для Mini App и полностью запретить фреймы для предотвращения атак
    ///     clickjacking.
    /// </summary>
    /// <param name="app">Построитель приложения.</param>
    /// <param name="allowedOrigins">Дополнительные origin'ы, разрешённые в Content-Security-Policy.</param>
    public static IApplicationBuilder UseStrictCspForWebApp(
        this IApplicationBuilder app,
        IEnumerable<string> allowedOrigins)
    {
        var joined = allowedOrigins?.Any() == true
            ? string.Join(' ', allowedOrigins)
            : string.Empty;

        app.Use(async (context, next) =>
        {
            var csp = $"default-src 'self' https://telegram.org https://*.telegram.org {joined}".Trim();
            context.Response.Headers.Append("Content-Security-Policy", $"{csp};");
            context.Response.Headers.Append("Referrer-Policy", "no-referrer");
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            await next();
        });

        return app;
    }
}
