using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions.Contracts;

namespace Bot.TestKit;

/// <summary>
///     Простейшее хранилище состояний в памяти.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Использует <see cref="ConcurrentDictionary{TKey, TValue}"/></item>
///         <item>Поддерживает TTL для записей</item>
///     </list>
/// </remarks>
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
    public Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var k = (scope, key);
        if (_store.TryGetValue(k, out var entry))
        {
            if (entry.expires is { } exp && exp <= DateTimeOffset.UtcNow)
            {
                _store.TryRemove(k, out _);
                return Task.FromResult(false);
            }

            var current = (T)entry.value;
            if (EqualityComparer<T>.Default.Equals(current, expected))
            {
                var newExp = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : entry.expires;
                _store.TryUpdate(k, (value!, newExp), entry);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var removed = _store.TryRemove((scope, key), out _);
        return Task.FromResult(removed);
    }

    /// <summary>
    ///     Увеличить числовое значение
    /// </summary>
    public async Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var current = await GetAsync<long>(scope, key, ct).ConfigureAwait(false);
        current += value;
        await SetAsync(scope, key, current, ttl, ct).ConfigureAwait(false);
        return current;
    }

    /// <summary>
    ///     Установить значение, если ключ отсутствует
    /// </summary>
    public async Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var existing = await GetAsync<T>(scope, key, ct).ConfigureAwait(false);
        if (existing is not null && !existing.Equals(default(T)))
        {
            return false;
        }
        await SetAsync(scope, key, value, ttl, ct).ConfigureAwait(false);
        return true;
    }
}
