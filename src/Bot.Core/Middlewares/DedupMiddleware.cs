using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Stats;
using Bot.Core.Utils;

using Microsoft.Extensions.Logging;

namespace Bot.Core.Middlewares;

/// <summary>
///     Избавление от повторений.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Отбрасывает обновления с уже виденным идентификатором.</item>
///         <item>Пишет предупреждение в лог.</item>
///         <item>Учитывает пропуски в статистике.</item>
///     </list>
/// </remarks>
public sealed class DedupMiddleware(ILogger<DedupMiddleware> logger, TtlCache<string> cache, StatsCollector stats) : IUpdateMiddleware
{
    /// <inheritdoc />
    public Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var added = cache.TryAdd(ctx.UpdateId);
        if (!added)
        {
            logger.LogWarning("duplicate update {UpdateId} ignored", ctx.UpdateId);
            stats.MarkDroppedUpdate();
            return Task.CompletedTask;
        }

        return next(ctx);
    }
}
