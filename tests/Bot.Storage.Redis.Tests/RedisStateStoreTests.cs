using FluentAssertions;

using StackExchange.Redis;

using Xunit;

namespace Bot.Storage.Redis.Tests;

/// <summary>
///     Тесты Redis-хранилища.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется инкремент с TTL</item>
///         <item>Проверяется условная установка</item>
///     </list>
/// </remarks>
public sealed class RedisStateStoreTests : IClassFixture<RedisFixture>
{
    private readonly RedisStateStore _store;
    private readonly IDatabase _db;

    /// <inheritdoc/>
    public RedisStateStoreTests(RedisFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture.Connection);

        var options = new RedisOptions { Connection = fixture.Connection };
        _store = new RedisStateStore(options);
        _db = fixture.Connection.GetDatabase();
    }

    /// <summary>
    ///     Тест 1: Проверяем инкремент и TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Проверяем инкремент и TTL")]
    public async Task IncrementAndTtl()
    {
        await _db.KeyDeleteAsync("s:k");
        var val = await _store.IncrementAsync("s", "k", 1, TimeSpan.FromSeconds(1), CancellationToken.None);
        val.Should().Be(1);
        await Task.Delay(3000);
        var exists = await _db.KeyExistsAsync("s:k");
        exists.Should().BeFalse();
    }

    /// <summary>
    ///     Тест 2: Проверяем условную установку.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Проверяем условную установку")]
    public async Task SetIfNotExists()
    {
        var set1 = await _store.SetIfNotExistsAsync("s", "k2", "v1", TimeSpan.FromSeconds(1), CancellationToken.None);
        var set2 = await _store.SetIfNotExistsAsync("s", "k2", "v2", TimeSpan.FromSeconds(1), CancellationToken.None);
        var value = await _store.GetAsync<string>("s", "k2", CancellationToken.None);
        set1.Should().BeTrue();
        set2.Should().BeFalse();
        value.Should().Be("v1");
    }
}
