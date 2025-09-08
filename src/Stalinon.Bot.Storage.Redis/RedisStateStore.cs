using System.Text.Json;

using StackExchange.Redis;

using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Storage.Redis;

/// <summary>
///     Хранилище состояний в Redis.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Использует Redis для хранения данных</item>
///         <item>Поддерживает TTL и атомарные операции</item>
///     </list>
/// </remarks>
public sealed class RedisStateStore : IStateStore
{
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _json;
    private readonly string _prefix;

    /// <summary>
    ///     Создаёт хранилище Redis.
    /// </summary>
    /// <param name="options">Опции Redis</param>
    public RedisStateStore(RedisOptions options)
    {
        _db = options.Connection.GetDatabase(options.Database);
        _prefix = options.Prefix ?? string.Empty;
        _json = options.Serialization ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var val = await _db.StringGetAsync(MakeKey(scope, key)).ConfigureAwait(false);
        if (!val.HasValue)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(val!, _json);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = JsonSerializer.Serialize(value, _json);
        await _db.StringSetAsync(MakeKey(scope, key), json, ttl).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return await _db.KeyDeleteAsync(MakeKey(scope, key)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string script = @"local val = redis.call('INCRBY', KEYS[1], ARGV[1])
if tonumber(ARGV[2]) and tonumber(ARGV[2]) > 0 then
    redis.call('PEXPIRE', KEYS[1], ARGV[2])
end
return val";
        var keys = new RedisKey[] { MakeKey(scope, key) };
        var args = new RedisValue[] { value, ttl.HasValue ? (long)ttl.Value.TotalMilliseconds : -1 };
        var result = await _db.ScriptEvaluateAsync(script, keys, args).ConfigureAwait(false);
        return (long)result;
    }

    /// <inheritdoc />
    public async Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string script = @"if redis.call('EXISTS', KEYS[1]) == 0 then
    redis.call('SET', KEYS[1], ARGV[1])
    if tonumber(ARGV[2]) and tonumber(ARGV[2]) > 0 then
        redis.call('PEXPIRE', KEYS[1], ARGV[2])
    end
    return 1
end
return 0";
        var json = JsonSerializer.Serialize(value, _json);
        var keys = new RedisKey[] { MakeKey(scope, key) };
        var args = new RedisValue[] { json, ttl.HasValue ? (long)ttl.Value.TotalMilliseconds : -1 };
        var result = await _db.ScriptEvaluateAsync(script, keys, args).ConfigureAwait(false);
        return (int)result == 1;
    }

    /// <summary>
    ///     Установить значение, если текущее совпадает с ожидаемым.
    /// </summary>
    /// <inheritdoc />
    public async Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string script = @"local cur = redis.call('GET', KEYS[1])
if cur == ARGV[1] then
    redis.call('SET', KEYS[1], ARGV[2])
    if tonumber(ARGV[3]) and tonumber(ARGV[3]) > 0 then
        redis.call('PEXPIRE', KEYS[1], ARGV[3])
    end
    return 1
end
return 0";
        var jsonExpected = JsonSerializer.Serialize(expected, _json);
        var jsonValue = JsonSerializer.Serialize(value, _json);
        var keys = new RedisKey[] { MakeKey(scope, key) };
        var args = new RedisValue[] { jsonExpected, jsonValue, ttl.HasValue ? (long)ttl.Value.TotalMilliseconds : -1 };
        var result = await _db.ScriptEvaluateAsync(script, keys, args).ConfigureAwait(false);
        return (int)result == 1;
    }

    private string MakeKey(string scope, string key)
    {
        return string.IsNullOrEmpty(_prefix)
            ? $"{scope}:{key}"
            : $"{_prefix}:{scope}:{key}";
    }
}
