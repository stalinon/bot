using System.Collections.Concurrent;

namespace Bot.Core.Utils;

/// <summary>
///     Простой TTL-кеш на базе <see cref="ConcurrentDictionary{TKey,TValue}" />.
///     Записи старше указанного TTL периодически удаляются.
/// </summary>
public sealed class TtlCache<TKey> : IDisposable where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, DateTimeOffset> _entries = new();
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
        return _entries.TryAdd(key, DateTimeOffset.UtcNow);
    }

    private void Cleanup()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, created) in _entries)
        {
            if (now - created >= Ttl)
            {
                _entries.TryRemove(key, out _);
            }
        }
    }
}
