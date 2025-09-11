using FluentAssertions;

using StackExchange.Redis;

using Xunit;

namespace Stalinon.Bot.Storage.Redis.Tests;

/// <summary>
///     Тесты RedisStateStore: проверка записи, чтения, конкурентных обновлений и очистки.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется запись и чтение значения</item>
///         <item>Проверяется корректность атомарного инкремента при параллельных обновлениях</item>
///         <item>Проверяется удаление значения</item>
///         <item>Проверяется истечение TTL</item>
///         <item>Проверяется работа со списками</item>
///     </list>
/// </remarks>
public sealed class RedisStateStoreTests
{
    /// <inheritdoc />
    public RedisStateStoreTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен записывать и читать значение.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен записывать и читать значение.")]
    public async Task Should_WriteAndReadValue_When_SetAndGet()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "state" };
        var sut = new RedisStateStore(options);
        const string scope = "read";
        const string key = "player";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("state:read:player");
        var player = new Player("Alice");

        // Act
        await sut.SetAsync(scope, key, player, null, CancellationToken.None);
        var fetched = await sut.GetAsync<Player>(scope, key, CancellationToken.None);
        mux.Dispose();

        // Assert
        fetched.Should().Be(player);
    }

    /// <summary>
    ///     Тест 2: Должен атомарно инкрементировать при параллельных обновлениях.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен атомарно инкрементировать при параллельных обновлениях.")]
    public async Task Should_IncrementAtomically_When_Concurrent()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "state" };
        var sut = new RedisStateStore(options);
        const string scope = "inc";
        const string key = "count";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("state:inc:count");

        // Act
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => sut.IncrementAsync(scope, key, 1, null, CancellationToken.None));
        var results = await Task.WhenAll(tasks);
        mux.Dispose();

        // Assert
        results.Last().Should().Be(50);
    }

    /// <summary>
    ///     Тест 3: Должен удалять значение.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен удалять значение.")]
    public async Task Should_RemoveValue_When_Called()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "state" };
        var sut = new RedisStateStore(options);
        const string scope = "del";
        const string key = "player";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("state:del:player");
        await sut.SetAsync(scope, key, new Player("Bob"), null, CancellationToken.None);

        // Act
        var removed = await sut.RemoveAsync(scope, key, CancellationToken.None);
        var fetched = await sut.GetAsync<Player>(scope, key, CancellationToken.None);
        mux.Dispose();

        // Assert
        removed.Should().BeTrue();
        fetched.Should().BeNull();
    }

    /// <summary>
    ///     Тест 4: Должен удалять значение после истечения TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен удалять значение после истечения TTL.")]
    public async Task Should_Remove_When_TtlExpired()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "state" };
        var sut = new RedisStateStore(options);
        const string scope = "ttl";
        const string key = "temp";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("state:ttl:temp");
        await sut.SetAsync(scope, key, new Player("Temp"), TimeSpan.FromMilliseconds(100), CancellationToken.None);

        // Act
        await Task.Delay(200);
        var fetched = await sut.GetAsync<Player>(scope, key, CancellationToken.None);
        mux.Dispose();

        // Assert
        fetched.Should().BeNull();
    }

    /// <summary>
    ///     Тест 5: Должен сериализовывать и десериализовывать список.
    /// </summary>
    [Fact(DisplayName = "Тест 5: Должен сериализовывать и десериализовывать список.")]
    public async Task Should_HandleLists()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "state" };
        var sut = new RedisStateStore(options);
        const string scope = "list";
        const string key = "players";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("state:list:players");
        var players = new List<Player> { new("Ann"), new("Bob") };

        // Act
        await sut.SetAsync(scope, key, players, null, CancellationToken.None);
        var fetched = await sut.GetAsync<List<Player>>(scope, key, CancellationToken.None);
        mux.Dispose();

        // Assert
        fetched.Should().BeEquivalentTo(players, o => o.WithStrictOrdering());
    }
}

