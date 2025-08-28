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
    ///     Удалить
    /// </summary>
    Task<bool> RemoveAsync(string scope, string key, CancellationToken ct);
}