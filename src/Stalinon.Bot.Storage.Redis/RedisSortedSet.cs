using System.Text.Json;

using StackExchange.Redis;

namespace Stalinon.Bot.Storage.Redis;

/// <summary>
///     Отсортированное множество в Redis.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Сериализует значения перед сохранением</item>
///         <item>Использует префикс ключей из настроек</item>
///     </list>
/// </remarks>
public sealed class RedisSortedSet<T>
{
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _json;
    private readonly string _prefix;

    /// <summary>
    ///     Создаёт отсортированное множество Redis.
    /// </summary>
    /// <param name="options">Опции Redis.</param>
    public RedisSortedSet(RedisOptions options)
    {
        _db = options.Connection.GetDatabase(options.Database);
        _prefix = options.Prefix ?? string.Empty;
        _json = options.Serialization ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    /// <summary>
    ///     Добавить значение с указанным счётом.
    /// </summary>
    public async Task AddAsync(string key, T value, double score, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = JsonSerializer.Serialize(value, _json);
        await _db.SortedSetAddAsync(MakeKey(key), json, score).ConfigureAwait(false);
    }

    /// <summary>
    ///     Удалить значение из множества.
    /// </summary>
    public async Task<bool> RemoveAsync(string key, T value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = JsonSerializer.Serialize(value, _json);
        return await _db.SortedSetRemoveAsync(MakeKey(key), json).ConfigureAwait(false);
    }

    /// <summary>
    ///     Получить значения в заданном диапазоне счёта.
    /// </summary>
    public async Task<IReadOnlyList<T>> RangeByScoreAsync(string key, double start, double stop, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var values = await _db.SortedSetRangeByScoreAsync(MakeKey(key), start, stop).ConfigureAwait(false);
        if (values.Length == 0)
        {
            return [];
        }

        var result = new List<T>(values.Length);
        foreach (var val in values)
        {
            var item = JsonSerializer.Deserialize<T>(val!, _json);
            if (item != null)
            {
                result.Add(item);
            }
        }

        return result;
    }

    private string MakeKey(string key)
    {
        return string.IsNullOrEmpty(_prefix) ? key : $"{_prefix}:{key}";
    }
}
