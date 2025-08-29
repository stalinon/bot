using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions.Contracts;

namespace Bot.TestKit;

/// <summary>
///     Простейшее хранилище состояний в памяти.
/// </summary>
public sealed class InMemoryStateStore : IStateStore
{
    private readonly ConcurrentDictionary<(string scope, string key), (object value, DateTimeOffset? expires)> _store = new();

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (_store.TryGetValue((scope, key), out var entry))
        {
            if (entry.expires is { } exp && exp <= DateTimeOffset.UtcNow)
            {
                _store.TryRemove((scope, key), out _);
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult((T?)entry.value);
        }

        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var exp = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : (DateTimeOffset?)null;
        _store[(scope, key)] = (value!, exp);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var removed = _store.TryRemove((scope, key), out _);
        return Task.FromResult(removed);
    }
}
