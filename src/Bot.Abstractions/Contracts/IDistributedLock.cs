namespace Bot.Abstractions.Contracts;

/// <summary>
///     Распределённый лок.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Позволяет захватывать и освобождать ресурс по ключу</item>
///         <item>Использует TTL для автоматического снятия</item>
///     </list>
/// </remarks>
public interface IDistributedLock
{
    /// <summary>
    ///     Попытаться захватить лок.
    /// </summary>
    /// <param name="key">Ключ лока.</param>
    /// <param name="ttl">Время жизни лока.</param>
    /// <param name="ct">Токен отмены.</param>
    Task<bool> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct);

    /// <summary>
    ///     Освободить лок.
    /// </summary>
    /// <param name="key">Ключ лока.</param>
    /// <param name="ct">Токен отмены.</param>
    Task ReleaseAsync(string key, CancellationToken ct);
}
