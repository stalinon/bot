using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions.Contracts;

namespace Bot.TestKit;

/// <summary>
///     Простейший распределённый лок в памяти.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Хранит ключи и сроки истечения в словаре</item>
///         <item>Учитывает TTL при захвате и освобождении</item>
///     </list>
/// </remarks>
public sealed class FakeDistributedLock : IDistributedLock
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _locks = new();

    /// <inheritdoc />
    public Task<bool> AcquireAsync(string key, TimeSpan ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(ttl);
        while (true)
        {
            if (_locks.TryGetValue(key, out var current))
            {
                if (current <= now && _locks.TryUpdate(key, expires, current))
                {
                    return Task.FromResult(true);
                }

                return Task.FromResult(false);
            }

            if (_locks.TryAdd(key, expires))
            {
                return Task.FromResult(true);
            }
        }
    }

    /// <inheritdoc />
    public Task ReleaseAsync(string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _locks.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
