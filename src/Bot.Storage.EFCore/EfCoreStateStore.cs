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

        var now = DateTimeOffset.UtcNow;
        var expiresAt = ttl.HasValue ? now.Add(ttl.Value) : (DateTimeOffset?)null;

        FormattableString sql = $"""
INSERT INTO states ("Scope", "Key", "Value", "ExpiresAt")
VALUES ({scope}, {key}, {value.ToString()}, {expiresAt})
ON CONFLICT ("Scope", "Key") DO UPDATE SET
    "Value" = CASE
        WHEN states."ExpiresAt" IS NULL OR states."ExpiresAt" > {now}
            THEN (CAST(states."Value" AS bigint) + {value})::text
        ELSE {value.ToString()}
    END,
    "ExpiresAt" = {expiresAt}
RETURNING CAST("Value" AS bigint);
""";

        var result = await _db.Database.SqlQuery<long>(sql).SingleAsync(ct).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> SetIfNotExistsAsync<T>(string scope, string key, T value, TimeSpan? ttl, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var now = DateTimeOffset.UtcNow;
        var expiresAt = ttl.HasValue ? now.Add(ttl.Value) : (DateTimeOffset?)null;
        var json = JsonSerializer.Serialize(value, Json);

        FormattableString sql = $"""
INSERT INTO states ("Scope", "Key", "Value", "ExpiresAt")
VALUES ({scope}, {key}, {json}, {expiresAt})
ON CONFLICT ("Scope", "Key") DO UPDATE SET
    "Value" = EXCLUDED."Value",
    "ExpiresAt" = EXCLUDED."ExpiresAt"
WHERE states."ExpiresAt" IS NULL OR states."ExpiresAt" <= {now};
""";

        var affected = await _db.Database.ExecuteSqlInterpolatedAsync(sql, ct).ConfigureAwait(false);

        return affected > 0;
    }
}
