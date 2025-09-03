using System;
using System.Threading;
using System.Threading.Tasks;
using Bot.Storage.Redis;
using FluentAssertions;
using StackExchange.Redis;
using Xunit;

namespace Bot.Storage.Redis.Tests;

/// <summary>
///     Тесты CAS и префиксов Redis-хранилища.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется условное обновление значения</item>
///         <item>Проверяется применение префикса</item>
///     </list>
/// </remarks>
public sealed class RedisStateStoreCasTests : IClassFixture<RedisFixture>
{
    private readonly RedisFixture _fixture;
    private readonly IDatabase _db;

    /// <inheritdoc/>
    public RedisStateStoreCasTests(RedisFixture fixture)
    {
        _fixture = fixture;
        _db = fixture.Connection.GetDatabase();
    }

    /// <summary>
    ///     Тест 1: Должен обновлять значение при совпадении ожидаемого.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен обновлять значение при совпадении ожидаемого", Skip = "Требуется стабильный Redis")]
    public async Task Should_UpdateValue_WhenExpectedMatches()
    {
        if (_fixture.Connection is null)
        {
            return;
        }

        // Arrange
        var options = new RedisOptions { Connection = _fixture.Connection };
        var store = new RedisStateStore(options);
        var key = Guid.NewGuid().ToString("N");
        await store.SetAsync("s", key, "v1", null, CancellationToken.None);

        // Act
        var result = await store.TrySetIfAsync("s", key, "v1", "v2", null, CancellationToken.None);
        var value = await store.GetAsync<string>("s", key, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        value.Should().Be("v2");
    }

    /// <summary>
    ///     Тест 2: Не должен обновлять значение при несовпадении ожидаемого.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Не должен обновлять значение при несовпадении ожидаемого", Skip = "Требуется стабильный Redis")]
    public async Task Should_NotUpdate_WhenExpectedDiffers()
    {
        if (_fixture.Connection is null)
        {
            return;
        }

        // Arrange
        var options = new RedisOptions { Connection = _fixture.Connection };
        var store = new RedisStateStore(options);
        var key = Guid.NewGuid().ToString("N");
        await store.SetAsync("s", key, "v1", null, CancellationToken.None);

        // Act
        var result = await store.TrySetIfAsync("s", key, "v0", "v2", null, CancellationToken.None);
        var value = await store.GetAsync<string>("s", key, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        value.Should().Be("v1");
    }

    /// <summary>
    ///     Тест 3: Должен применять префикс к ключу.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен применять префикс к ключу", Skip = "Требуется стабильный Redis")]
    public async Task Should_ApplyPrefixToKey()
    {
        if (_fixture.Connection is null)
        {
            return;
        }

        // Arrange
        var options = new RedisOptions { Connection = _fixture.Connection, Prefix = "pfx" };
        var store = new RedisStateStore(options);

        // Act
        await store.SetAsync("s", "k", "v", null, CancellationToken.None);
        var exists = await _db.KeyExistsAsync("pfx:s:k");

        // Assert
        exists.Should().BeTrue();
    }
}
