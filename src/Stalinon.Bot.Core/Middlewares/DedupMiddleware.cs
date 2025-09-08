using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Core.Utils;

namespace Stalinon.Bot.Core.Middlewares;

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
    IOptions<DeduplicationOptions> options,
    StatsCollector stats,
    IStateStore? store = null) : IUpdateMiddleware, IDisposable
{
    private readonly DeduplicationOptions _options = options.Value;
    private readonly TtlCache<string> _cache = new(options.Value.Window);

    /// <inheritdoc />
    public async ValueTask InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        if (store is not null)
        {
            var added = await store.SetIfNotExistsAsync("dedup", ctx.UpdateId, 1, _cache.Ttl, ctx.CancellationToken)
                .ConfigureAwait(false);
            if (!added)
            {
                if (_options.Mode == RateLimitMode.Soft)
                {
                    logger.LogWarning("duplicate update {UpdateId} ignored", ctx.UpdateId);
                }

                stats.MarkDroppedUpdate("dedup");
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
                    logger.LogWarning("duplicate update {UpdateId} ignored", ctx.UpdateId);
                }

                stats.MarkDroppedUpdate("dedup");
                return;
            }
        }

        await next(ctx).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cache.Dispose();
    }
}
