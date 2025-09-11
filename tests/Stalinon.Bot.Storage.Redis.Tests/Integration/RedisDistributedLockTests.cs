using FluentAssertions;

using StackExchange.Redis;

using Xunit;

namespace Stalinon.Bot.Storage.Redis.Tests;

/// <summary>
///     Тесты RedisDistributedLock: проверка корректного удержания и освобождения локов.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется невозможность повторного захвата активного лока</item>
///         <item>Проверяется освобождение лока и повторный захват</item>
///         <item>Проверяется автоматическое освобождение по TTL</item>
///         <item>Проверяется выброс исключения при отсутствии подключения</item>
///     </list>
/// </remarks>
public sealed class RedisDistributedLockTests
{
    /// <inheritdoc />
    public RedisDistributedLockTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен возвращать false при попытке повторного захвата.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен возвращать false при попытке повторного захвата.")]
    public async Task Should_ReturnFalse_When_LockHeld()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "lock" };
        var sut = new RedisDistributedLock(options);
        const string key = "test";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("lock:test");

        // Act
        var first = await sut.AcquireAsync(key, TimeSpan.FromSeconds(10), CancellationToken.None);
        var second = await sut.AcquireAsync(key, TimeSpan.FromSeconds(10), CancellationToken.None);
        mux.Dispose();

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    /// <summary>
    ///     Тест 2: Должен освобождать лок и позволять повторный захват.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен освобождать лок и позволять повторный захват.")]
    public async Task Should_AcquireAgain_After_Release()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "lock" };
        var sut = new RedisDistributedLock(options);
        const string key = "again";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("lock:again");
        await sut.AcquireAsync(key, TimeSpan.FromSeconds(10), CancellationToken.None);

        // Act
        await sut.ReleaseAsync(key, CancellationToken.None);
        var reacquired = await sut.AcquireAsync(key, TimeSpan.FromSeconds(10), CancellationToken.None);
        mux.Dispose();

        // Assert
        reacquired.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 3: Должен освобождать лок по истечении TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен освобождать лок по истечении TTL.")]
    public async Task Should_Release_When_TtlExpires()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "lock" };
        var sut = new RedisDistributedLock(options);
        const string key = "ttl";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("lock:ttl");
        await sut.AcquireAsync(key, TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Act
        await Task.Delay(200);
        var reacquired = await sut.AcquireAsync(key, TimeSpan.FromMilliseconds(100), CancellationToken.None);
        mux.Dispose();

        // Assert
        reacquired.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 4: Должен выбрасывать исключение при отсутствии подключения.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен выбрасывать исключение при отсутствии подключения.")]
    public void Should_Throw_When_ConnectionMissing()
    {
        // Arrange
        var options = new RedisOptions();

        // Act
        var act = () => new RedisDistributedLock(options);

        // Assert
        act.Should().Throw<NullReferenceException>();
    }
}
