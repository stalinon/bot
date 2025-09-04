using System.Diagnostics;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Stats;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Middlewares;

/// <summary>
///     Логирование
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Добавляет идентификаторы обновления в scope</item>
///         <item>Замеряет длительность работы обработчика и ошибки</item>
///     </list>
/// </remarks>
public sealed class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : IUpdateMiddleware
{
    private const int TextLimit = 128;

    /// <inheritdoc />
    public async Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var updateType = ctx.GetItem<string>(UpdateItems.UpdateType) ?? "unknown";
        var messageId = ctx.GetItem<int?>(UpdateItems.MessageId);
        var text = ctx.Text;
        if (text is not null && text.Length > TextLimit)
        {
            text = text[..TextLimit];
        }

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["UpdateId"] = ctx.UpdateId,
            ["ChatId"] = ctx.Chat.Id,
            ["UserId"] = ctx.User.Id,
            ["MessageId"] = messageId,
            ["UpdateType"] = updateType,
            ["Text"] = text
        });

        logger.LogInformation("update");

        var sw = Stopwatch.StartNew();
        try
        {
            await next(ctx);
            var handler = ctx.GetItem<string>(UpdateItems.Handler) ?? "unknown";
            logger.LogInformation("handler {Handler} finished in {DurationMs}ms", handler, sw.ElapsedMilliseconds);
            LogWebAppData(sw.ElapsedMilliseconds, true);
        }
        catch (Exception ex)
        {
            var handler = ctx.GetItem<string>(UpdateItems.Handler) ?? "unknown";
            logger.LogError(ex, "handler {Handler} failed in {DurationMs}ms", handler, sw.ElapsedMilliseconds);
            LogWebAppData(sw.ElapsedMilliseconds, false);
            throw;
        }

        void LogWebAppData(long latency, bool success)
        {
            if (ctx.GetItem<bool>(UpdateItems.WebAppData))
            {
                logger.LogInformation(
                    "web_app_data handled for {webapp_user_id} from {source} in {latency}ms",
                    ctx.User.Id,
                    "miniapp",
                    latency);
                var stats = ctx.Services.GetService<WebAppStatsCollector>();
                stats?.MarkSendData(latency, success);
            }
        }
    }
}
