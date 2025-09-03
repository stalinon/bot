using StackExchange.Redis;
using Xunit;

namespace Bot.Storage.Redis.Tests;

/// <summary>
///     Тесты Redis-хранилища
/// </summary>
public sealed class RedisStateStoreTests : IClassFixture<RedisFixture>
{
    private readonly RedisStateStore _store;
    private readonly IDatabase _db;

    /// <summary>
    ///     Создаёт тесты
    /// </summary>
    /// <param name="fixture">Фикстура Redis</param>
    public RedisStateStoreTests(RedisFixture fixture)
    {
        _store = new RedisStateStore(fixture.Connection);
        _db = fixture.Connection.GetDatabase();
    }

    /// <summary>
    ///     Тест 1. Проверяем инкремент и TTL
    /// </summary>
    [Fact(DisplayName = "Тест 1. Проверяем инкремент и TTL", Skip = "Требуется стабильный Redis")]
    public async Task IncrementAndTtl()
    {
        await _db.KeyDeleteAsync("s:k");
        var val = await _store.IncrementAsync("s", "k", 1, TimeSpan.FromMilliseconds(200), CancellationToken.None);
        Assert.Equal(1, val);
        await Task.Delay(1500);
        var exists = await _db.KeyExistsAsync("s:k");
        Assert.False(exists);
    }

    /// <summary>
    ///     Тест 2. Проверяем условную установку
    /// </summary>
    [Fact(DisplayName = "Тест 2. Проверяем условную установку")]
    public async Task SetIfNotExists()
    {
        var set1 = await _store.SetIfNotExistsAsync("s", "k2", "v1", TimeSpan.FromSeconds(1), CancellationToken.None);
        var set2 = await _store.SetIfNotExistsAsync("s", "k2", "v2", TimeSpan.FromSeconds(1), CancellationToken.None);
        var value = await _store.GetAsync<string>("s", "k2", CancellationToken.None);
        Assert.True(set1);
        Assert.False(set2);
        Assert.Equal("v1", value);
    }
}
