using Bot.Abstractions.Contracts;

namespace Bot.Observability;

/// <summary>
///     Декоратор хранилища состояний с трассировкой.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Создаёт спаны операций чтения и записи.</item>
///     </list>
/// </remarks>
public sealed class TracingStateStore(IStateStore inner) : IStateStore
{
    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Store/Get");
        activity?.SetTag("scope", scope);
        activity?.SetTag("key", key);
        return inner.GetAsync<T>(scope, key, ct);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Store/Set");
        activity?.SetTag("scope", scope);
        activity?.SetTag("key", key);
        activity?.SetTag("ttl", ttl?.TotalSeconds);
        return inner.SetAsync(scope, key, value, ttl, ct);
    }

    /// <inheritdoc />
    public Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl, CancellationToken ct)
    {
        return inner.TrySetIfAsync(scope, key, expected, value, ttl, ct);
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        return inner.RemoveAsync(scope, key, ct);
    }

    /// <inheritdoc />
    public Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
    {
        return inner.IncrementAsync(scope, key, value, ttl, ct);
    }

    /// <inheritdoc />
    public Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        return inner.SetIfNotExistsAsync(scope, key, value, ttl, ct);
    }
}
