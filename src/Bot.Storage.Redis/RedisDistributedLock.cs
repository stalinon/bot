using Bot.Abstractions.Contracts;

using StackExchange.Redis;

namespace Bot.Storage.Redis;

/// <summary>
///     Распределённый лок в Redis.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Использует команду SETNX для захвата ключа</item>
///         <item>Устанавливает TTL для автоматического освобождения</item>
///     </list>
/// </remarks>
public sealed class RedisDistributedLock : IDistributedLock
{
    private readonly IDatabase _db;
    private readonly string _prefix;

    /// <summary>
    ///     Создаёт Redis-лок.
    /// </summary>
    /// <param name="options">Опции Redis.</param>
    public RedisDistributedLock(RedisOptions options)
    {
        _db = options.Connection.GetDatabase(options.Database);
        _prefix = options.Prefix ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<bool> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await _db.StringSetAsync(MakeKey(key), "1", ttl, When.NotExists).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task ReleaseAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _db.KeyDeleteAsync(MakeKey(key)).ConfigureAwait(false);
    }

    private string MakeKey(string key)
    {
        return string.IsNullOrEmpty(_prefix) ? key : $"{_prefix}:{key}";
    }
}
