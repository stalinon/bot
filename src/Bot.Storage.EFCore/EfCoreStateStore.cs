using System;
using System.Text.Json;
using Bot.Abstractions.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bot.Storage.EFCore;

/// <summary>
///     Хранилище состояний на EF Core.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Использует базу данных для хранения состояния</item>
///         <item>Выполняет миграции при инициализации</item>
///     </list>
/// </remarks>
public sealed class EfCoreStateStore : IStateStore
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
        if (entity.TtlUtc is { } exp && exp <= DateTimeOffset.UtcNow)
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
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        entity.TtlUtc = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : null;
        entity.Version++;
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
        else if (entity.TtlUtc is { } exp && exp <= DateTimeOffset.UtcNow)
        {
            entity.Value = "0";
        }
        current = long.Parse(entity.Value) + value;
        entity.Value = current.ToString();
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        entity.TtlUtc = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : null;
        entity.Version++;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return current;
    }

    /// <inheritdoc />
    public async Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await _db.States.FindAsync(new object?[] { scope, key }, ct).ConfigureAwait(false);
        if (entity is not null && (entity.TtlUtc is null || entity.TtlUtc > DateTimeOffset.UtcNow))
        {
            return false;
        }
        if (entity is null)
        {
            entity = new StateEntry { Scope = scope, Key = key, Value = string.Empty };
            await _db.States.AddAsync(entity, ct).ConfigureAwait(false);
        }
        entity.Value = JsonSerializer.Serialize(value, Json);
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        entity.TtlUtc = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : null;
        entity.Version++;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    ///     Установить значение, если текущее совпадает с ожидаемым.
    /// </summary>
    /// <inheritdoc />
    public async Task<bool> TrySetIfAsync<T>(string scope, string key, T expected, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var entity = await _db.States.FindAsync(new object?[] { scope, key }, ct).ConfigureAwait(false);
        if (entity is null)
        {
            return false;
        }

        if (entity.TtlUtc is { } exp && exp <= DateTimeOffset.UtcNow)
        {
            _db.States.Remove(entity);
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return false;
        }

        entity.Value = JsonSerializer.Serialize(value, Json);
        entity.UpdatedUtc = DateTimeOffset.UtcNow;
        entity.TtlUtc = ttl.HasValue ? DateTimeOffset.UtcNow.Add(ttl.Value) : entity.TtlUtc;
        entity.Version++;

        try
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
