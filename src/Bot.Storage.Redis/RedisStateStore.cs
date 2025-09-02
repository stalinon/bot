using System.Text.Json;
using Bot.Abstractions.Contracts;
using StackExchange.Redis;

namespace Bot.Storage.Redis;

/// <summary>
///     Хранилище состояний в Redis
/// </summary>
public sealed class RedisStateStore : IStateStorage
{
    private readonly IDatabase _db;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///     Создаёт хранилище Redis
    /// </summary>
    /// <param name="connection">Подключение к Redis</param>
    public RedisStateStore(IConnectionMultiplexer connection)
    {
        _db = connection.GetDatabase();
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
        return JsonSerializer.Deserialize<T>(val!, Json);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = JsonSerializer.Serialize(value, Json);
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
    public async Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
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
        var json = JsonSerializer.Serialize(value, Json);
        var keys = new RedisKey[] { MakeKey(scope, key) };
        var args = new RedisValue[] { json, ttl.HasValue ? (long)ttl.Value.TotalMilliseconds : -1 };
        var result = await _db.ScriptEvaluateAsync(script, keys, args).ConfigureAwait(false);
        return (int)result == 1;
    }

    private static string MakeKey(string scope, string key) => $"{scope}:{key}";
}
