using System;
using System.Collections.Concurrent;
using System.Threading;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Options;
using Bot.Core.Stats;

using Microsoft.Extensions.Options;

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
public sealed class RateLimitMiddleware : IUpdateMiddleware, IDisposable
{
    private readonly ConcurrentDictionary<long, RingBuffer> _chat = new();
    private readonly ConcurrentDictionary<long, RingBuffer> _user = new();
    private readonly Timer _cleanup;
    private readonly RateLimitOptions _options;
    private readonly StatsCollector _stats;
    private readonly ITransportClient _tx;
    private readonly IStateStore? _store;

    /// <summary>
    ///     Инициализирует экземпляр <see cref="RateLimitMiddleware" />.
    /// </summary>
    public RateLimitMiddleware(IOptions<RateLimitOptions> options,
        ITransportClient tx,
        StatsCollector stats,
        IStateStore? store = null)
    {
        _options = options.Value;
        _tx = tx;
        _stats = stats;
        _store = store;
        _cleanup = new Timer(Cleanup, null, _options.Window, _options.Window);
    }

    /// <inheritdoc />
    public async Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var now = DateTimeOffset.UtcNow;
        if (!await CheckAsync(_user, ctx.User.Id, _options.PerUserPerMinute, now, "ratelimit:user", ctx.CancellationToken) ||
            !await CheckAsync(_chat, ctx.Chat.Id, _options.PerChatPerMinute, now, "ratelimit:chat", ctx.CancellationToken))
        {
            _stats.MarkRateLimited();
            if (_options.Mode == RateLimitMode.Soft)
            {
                await _tx.SendTextAsync(ctx.Chat, "помедленнее", ctx.CancellationToken);
            }

            return;
        }

        await next(ctx);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _cleanup.Dispose();
    }

    private async Task<bool> CheckAsync(ConcurrentDictionary<long, RingBuffer> dict, long key, int limit,
        DateTimeOffset now, string scope, CancellationToken ct)
    {
        if (_options.UseStateStore && _store is not null)
        {
            var count = await _store.IncrementAsync(scope, key.ToString(), 1, _options.Window, ct).ConfigureAwait(false);
            return count <= limit;
        }

        if (limit == int.MaxValue)
        {
            return true;
        }

        var buf = dict.GetOrAdd(key, _ => new RingBuffer(limit));
        lock (buf)
        {
            return buf.Add(now.Ticks, _options.Window.Ticks);
        }
    }

    private void Cleanup(object? state)
    {
        var threshold = DateTimeOffset.UtcNow.Ticks - _options.Window.Ticks;
        CleanupDict(_user, threshold);
        CleanupDict(_chat, threshold);
    }

    private static void CleanupDict(ConcurrentDictionary<long, RingBuffer> dict, long threshold)
    {
        foreach (var (key, buf) in dict)
        {
            if (buf.Last < threshold)
            {
                dict.TryRemove(key, out _);
            }
        }
    }

    private sealed class RingBuffer
    {
        private readonly long[] _items;
        private int _index;
        private int _count;

        public RingBuffer(int size)
        {
            _items = new long[size];
        }

        public bool Add(long now, long window)
        {
            if (_count < _items.Length)
            {
                _items[_index] = now;
                _index = (_index + 1) % _items.Length;
                _count++;
                return true;
            }

            var oldest = _items[_index];
            if (now - oldest < window)
            {
                return false;
            }

            _items[_index] = now;
            _index = (_index + 1) % _items.Length;
            return true;
        }

        public long Last => _count == 0 ? 0 : _items[(_index - 1 + _items.Length) % _items.Length];
    }
}
