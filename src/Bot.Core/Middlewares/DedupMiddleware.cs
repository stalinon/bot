using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Options;
using Bot.Core.Stats;
using Bot.Core.Utils;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
public sealed class DedupMiddleware(
    ILogger<DedupMiddleware> logger,
    TtlCache<string> cache,
    IOptions<DeduplicationOptions> options,
    StatsCollector stats,
    IStateStore? store = null) : IUpdateMiddleware
{
    private readonly DeduplicationOptions _options = options.Value;
    private readonly TtlCache<string> _cache = cache;

    /// <inheritdoc />
    public async Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        if (store is not null)
        {
            var added = await store.SetIfNotExistsAsync("dedup", ctx.UpdateId, 1, _cache.Ttl, ctx.CancellationToken)
                .ConfigureAwait(false);
            if (!added)
            {
                if (_options.Mode == RateLimitMode.Soft)
                {
                    logger.LogWarning("повторное обновление {UpdateId} проигнорировано", ctx.UpdateId);
                }
                stats.MarkDroppedUpdate();
                return;
            }
        }
        else
        {
            var added = _cache.TryAdd(ctx.UpdateId);
            if (!added)
            {
                if (_options.Mode == RateLimitMode.Soft)
                {
                    logger.LogWarning("повторное обновление {UpdateId} проигнорировано", ctx.UpdateId);
                }
                stats.MarkDroppedUpdate();
                return;
            }
        }

        await next(ctx);
    }
}
