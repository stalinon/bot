using System.Diagnostics;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Stats;

namespace Stalinon.Bot.Core.Middlewares;

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
    /// <inheritdoc />
    public async ValueTask InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var updateType = ctx.GetItem<string>(UpdateItems.UpdateType) ?? "unknown";
        var messageId = ctx.GetItem<int?>(UpdateItems.MessageId);

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["UpdateId"] = ctx.UpdateId,
            ["ChatId"] = ctx.Chat.Id,
            ["UserId"] = ctx.User.Id,
            ["MessageId"] = messageId,
            ["UpdateType"] = updateType,
            ["Text"] = ctx.Text,
            ["TraceId"] = Activity.Current?.TraceId.ToString()
        });

        logger.LogInformation("update");

        var sw = Stopwatch.StartNew();
        try
        {
            await next(ctx).ConfigureAwait(false);
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
