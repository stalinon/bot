using System.Collections.Concurrent;

namespace Stalinon.Bot.Core.Utils;

/// <summary>
///     Простой TTL-кеш с компактным хранением ключей и редкой уборкой.
/// </summary>
public sealed class TtlCache<TKey> : IDisposable where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, byte> _entries = new();
    private readonly ConcurrentQueue<(TKey Key, long Expire)> _queue = new();
    private readonly Timer _timer;

    /// <inheritdoc cref="TtlCache{TKey}" />
    public TtlCache(TimeSpan ttl)
    {
        Ttl = ttl;
        _timer = new Timer(_ => Cleanup(), null, ttl, ttl);
    }

    /// <summary>
    ///     Время жизни записей в кеше.
    /// </summary>
    public TimeSpan Ttl { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        _timer.Dispose();
    }

    /// <summary>
    ///     Пытается добавить ключ в кеш.
    /// </summary>
    /// <param name="key">Ключ для добавления.</param>
    /// <returns><c>true</c>, если ключ был добавлен; иначе <c>false</c>.</returns>
    public bool TryAdd(TKey key)
    {
        var added = _entries.TryAdd(key, 0);
        if (added)
        {
            var exp = DateTimeOffset.UtcNow.Add(Ttl).Ticks;
            _queue.Enqueue((key, exp));
        }

        return added;
    }

    private void Cleanup()
    {
        var now = DateTimeOffset.UtcNow.Ticks;
        while (_queue.TryPeek(out var item) && item.Expire <= now)
        {
            _queue.TryDequeue(out _);
            _entries.TryRemove(item.Key, out _);
        }
    }
}
