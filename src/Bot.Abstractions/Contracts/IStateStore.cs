namespace Bot.Abstractions.Contracts;

/// <summary>
///     Хранилище состояний
/// </summary>
public interface IStateStore
{
    /// <summary>
    ///     Получить
    /// </summary>
    Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct);
    
    /// <summary>
    ///     Установить
    /// </summary>
    Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct);

    /// <summary>
    ///     Установить, если текущее значение совпадает с ожидаемым.
    /// </summary>
    /// <param name="scope">Область.</param>
    /// <param name="key">Ключ.</param>
    /// <param name="expected">Ожидаемое значение.</param>
    /// <param name="value">Новое значение.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>
    ///     <c>true</c>, если значение было установлено; иначе <c>false</c>.
    /// </returns>
    Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, CancellationToken ct);
    
    /// <summary>
    ///     Удалить
    /// </summary>
    Task<bool> RemoveAsync(string scope, string key, CancellationToken ct);
}