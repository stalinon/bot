using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.TestKit;

namespace Stalinon.Bot.Scheduler.Tests;

/// <summary>
///     Атомарное хранилище состояний для тестов.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Использует блокировку для SetIfNotExistsAsync</item>
///         <item>Оборачивает <see cref="InMemoryStateStore" /></item>
///     </list>
/// </remarks>
internal sealed class AtomicStateStore : IStateStore
{
    private readonly InMemoryStateStore _inner = new();
    private readonly object _sync = new();

    public Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        return _inner.GetAsync<T>(scope, key, ct);
    }

    public Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        return _inner.SetAsync(scope, key, value, ttl, ct);
    }

    public Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl,
        CancellationToken ct)
    {
        return _inner.TrySetIfAsync(scope, key, expected, value, ttl, ct);
    }

    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        return _inner.RemoveAsync(scope, key, ct);
    }

    public Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
    {
        return _inner.IncrementAsync(scope, key, value, ttl, ct);
    }

    public Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        lock (_sync)
        {
            return _inner.SetIfNotExistsAsync(scope, key, value, ttl, ct);
        }
    }
}
