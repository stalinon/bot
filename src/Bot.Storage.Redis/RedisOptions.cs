using System.Text.Json;
using StackExchange.Redis;

namespace Bot.Storage.Redis;

/// <summary>
///     Опции Redis.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Определяют подключение и базу данных</item>
///         <item>Задают префикс ключей и параметры сериализации</item>
///     </list>
/// </remarks>
public sealed class RedisOptions
{
    /// <summary>
    ///     Подключение к Redis.
    /// </summary>
    public IConnectionMultiplexer Connection { get; init; } = null!;

    /// <summary>
    ///     Номер базы данных.
    /// </summary>
    public int Database { get; init; }

    /// <summary>
    ///     Префикс ключей.
    /// </summary>
    public string Prefix { get; init; } = string.Empty;

    /// <summary>
    ///     Опции сериализации.
    /// </summary>
    public JsonSerializerOptions Serialization { get; init; } = new(JsonSerializerDefaults.Web);
}
