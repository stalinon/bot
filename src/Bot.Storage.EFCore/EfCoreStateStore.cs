using System;
using System.Collections.Generic;
using System.Text.Json;
using Bot.Abstractions.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bot.Storage.EFCore;

/// <summary>
///     Хранилище состояний на EF Core
/// </summary>
public sealed class EfCoreStateStore : IStateStorage
{
    private readonly StateContext _db;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///     Создаёт EF Core хранилище
    /// </summary>
    /// <param name="db">Контекст базы данных</param>
    public EfCoreStateStore(StateContext db)
    {
        _db = db;
        _db.Database.Migrate();
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await _db.States.FindAsync(new object?[] { scope, key }, ct).ConfigureAwait(false);
        if (entity is null)
        {
            return default;
        }
        if (entity.ExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow)
        {
            _db.States.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return default;
        }
        return JsonSerializer.Deserialize<T>(entity.Value, Json);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await _db.States.FindAsync(new object?[] { scope, key }, ct).ConfigureAwait(false);
        if (entity is null)
        {
            entity = new StateEntry { Scope = scope, Key = key, Value = string.Empty };
            await _db.States.AddAsync(entity, ct).ConfigureAwait(false);
        }
        entity.Value = JsonSerializer.Serialize(value, Json);
        entity.ExpiresAt = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : null;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string scope, string key, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await _db.States.FindAsync(new object?[] { scope, key }, ct).ConfigureAwait(false);
        if (entity is null)
        {
            return false;
        }
        _db.States.Remove(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc />
    public async Task<long> IncrementAsync(string scope, string key, long value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await _db.States.FindAsync(new object?[] { scope, key }, ct).ConfigureAwait(false);
        long current = 0;
        if (entity is null)
        {
            entity = new StateEntry { Scope = scope, Key = key, Value = "0" };
            await _db.States.AddAsync(entity, ct).ConfigureAwait(false);
        }
        else if (entity.ExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow)
        {
            entity.Value = "0";
        }
        current = long.Parse(entity.Value) + value;
        entity.Value = current.ToString();
        entity.ExpiresAt = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : null;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return current;
    }

    /// <inheritdoc />
    public async Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await _db.States.FindAsync(new object?[] { scope, key }, ct).ConfigureAwait(false);
        if (entity is not null && (entity.ExpiresAt is null || entity.ExpiresAt > DateTimeOffset.UtcNow))
        {
            return false;
        }
        if (entity is null)
        {
            entity = new StateEntry { Scope = scope, Key = key, Value = string.Empty };
            await _db.States.AddAsync(entity, ct).ConfigureAwait(false);
        }
        entity.Value = JsonSerializer.Serialize(value, Json);
        entity.ExpiresAt = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : null;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Установить значение, если текущее совпадает с ожидаемым.
    /// </summary>
    /// <inheritdoc />
    public async Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await _db.States.FindAsync(new object?[] { scope, key }, ct).ConfigureAwait(false);
        if (entity is null)
        {
            return false;
        }

        if (entity.ExpiresAt is { } exp && exp <= DateTimeOffset.UtcNow)
        {
            _db.States.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return false;
        }

        var current = JsonSerializer.Deserialize<T>(entity.Value, Json);
        if (!EqualityComparer<T>.Default.Equals(current, expected))
        {
            return false;
        }

        entity.Value = JsonSerializer.Serialize(value, Json);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
