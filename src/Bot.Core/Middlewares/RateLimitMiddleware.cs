using System;
using System.Collections.Concurrent;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Options;
using Bot.Core.Stats;

namespace Bot.Core.Middlewares;

/// <summary>
///     Рейт-лиметер
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Ограничивает частоту запросов.</item>
///         <item>Отправляет предупреждение в мягком режиме.</item>
///         <item>Учитывает ограниченные обновления в статистике.</item>
///     </list>
/// </remarks>
public sealed class RateLimitMiddleware(RateLimitOptions options, ITransportClient tx, StatsCollector stats, IStateStore? store = null) : IUpdateMiddleware
{
    private readonly ConcurrentDictionary<long, Queue<DateTimeOffset>> _user = new();
    private readonly ConcurrentDictionary<long, Queue<DateTimeOffset>> _chat = new();
    private readonly IStateStore? _store = store;

    /// <inheritdoc />
    public async Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var now = DateTimeOffset.UtcNow;
        if (!await CheckAsync(_user, ctx.User.Id, options.PerUserPerMinute, now, "ratelimit:user", ctx.CancellationToken) ||
            !await CheckAsync(_chat, ctx.Chat.Id, options.PerChatPerMinute, now, "ratelimit:chat", ctx.CancellationToken))
        {
            stats.MarkRateLimited();
            if (options.Mode == RateLimitMode.Soft)
            {
                await tx.SendTextAsync(ctx.Chat, "помедленнее", ctx.CancellationToken);
            }

            return;
        }

        await next(ctx);
    }

    private async Task<bool> CheckAsync(ConcurrentDictionary<long, Queue<DateTimeOffset>> dict, long key, int limit, DateTimeOffset now, string scope, CancellationToken ct)
    {
        if (options.UseStateStore && _store is not null)
        {
            var count = await _store.IncrementAsync(scope, key.ToString(), 1, options.Window, ct).ConfigureAwait(false);
            return count <= limit;
        }

        return Check(dict, key, limit, now, options.Window);
    }

    private static bool Check(ConcurrentDictionary<long, Queue<DateTimeOffset>> dict, long key, int limit, DateTimeOffset now, TimeSpan window)
    {
        var q = dict.GetOrAdd(key, _ => new Queue<DateTimeOffset>());
        lock (q)
        {
            while (q.Count > 0 && now - q.Peek() > window)
            {
                q.Dequeue();
            }

            if (q.Count >= limit)
            {
                return false;
            }

            q.Enqueue(now);
            return true;
        }
    }
}
