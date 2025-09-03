namespace Bot.Abstractions.Contracts;

/// <summary>
///     Хранилище состояний
/// </summary>
public interface IStateStorage
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
    ///     Установить значение, если текущее совпадает с ожидаемым.
    /// </summary>
    /// <param name="scope">Область</param>
    /// <param name="key">Ключ</param>
    /// <param name="expected">Ожидаемое значение</param>
    /// <param name="value">Новое значение</param>
    /// <param name="ct">Токен отмены</param>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <returns>Возвращает <c>true</c>, если значение обновлено</returns>
    Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, CancellationToken ct);

    /// <summary>
    ///     Удалить
    /// </summary>
    Task<bool> RemoveAsync(string scope, string key, CancellationToken ct);

    /// <summary>
    ///     Увеличить числовое значение
    /// </summary>
    /// <param name="scope">Область</param>
    /// <param name="key">Ключ</param>
    /// <param name="value">Величина увеличения</param>
    /// <param name="ttl">Время жизни</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Новое значение</returns>
    Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct);

    /// <summary>
    ///     Установить значение, если ключ отсутствует
    /// </summary>
    /// <param name="scope">Область</param>
    /// <param name="key">Ключ</param>
    /// <param name="value">Значение</param>
    /// <param name="ttl">Время жизни</param>
    /// <param name="ct">Токен отмены</param>
    /// <typeparam name="T">Тип значения</typeparam>
    /// <returns>Возвращает <c>true</c>, если значение установлено</returns>
    Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct);
}
