using FluentAssertions;

using StackExchange.Redis;

using Xunit;

namespace Stalinon.Bot.Storage.Redis.Tests;

/// <summary>
///     Тесты RedisSortedSet: проверка операций добавления, удаления и выборки.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется сортировка по счёту</item>
///         <item>Проверяется удаление существующего значения</item>
///         <item>Проверяется пустой результат при отсутствии элементов</item>
///     </list>
/// </remarks>
public sealed class RedisSortedSetTests
{
    /// <inheritdoc />
    public RedisSortedSetTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен возвращать элементы упорядоченными по счёту.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен возвращать элементы упорядоченными по счёту.")]
    public async Task Should_ReturnOrdered_When_Added()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "sorted" };
        var sut = new RedisSortedSet<Player>(options);
        const string key = "leader";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("sorted:leader");

        var a = new Player("Alice");
        var b = new Player("Bob");
        await sut.AddAsync(key, b, 100, CancellationToken.None);
        await sut.AddAsync(key, a, 50, CancellationToken.None);

        // Act
        var items = await sut.RangeByScoreAsync(key, 0, 200, CancellationToken.None);
        mux.Dispose();

        // Assert
        items.Should().BeEquivalentTo([a, b], o => o.WithStrictOrdering());
    }

    /// <summary>
    ///     Тест 2: Должен удалять значение и возвращать true.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен удалять значение и возвращать true.")]
    public async Task Should_RemoveValue_When_Exists()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "sorted" };
        var sut = new RedisSortedSet<Player>(options);
        const string key = "remove";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("sorted:remove");

        var player = new Player("Solo");
        await sut.AddAsync(key, player, 42, CancellationToken.None);

        // Act
        var removed = await sut.RemoveAsync(key, player, CancellationToken.None);
        var items = await sut.RangeByScoreAsync(key, 0, 100, CancellationToken.None);
        mux.Dispose();

        // Assert
        removed.Should().BeTrue();
        items.Should().BeEmpty();
    }

    /// <summary>
    ///     Тест 3: Должен возвращать пустой список, если элементов нет.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен возвращать пустой список, если элементов нет.")]
    public async Task Should_ReturnEmpty_When_NoItems()
    {
        // Arrange
        var mux = await ConnectionMultiplexer.ConnectAsync("localhost");
        var options = new RedisOptions { Connection = mux, Database = 0, Prefix = "sorted" };
        var sut = new RedisSortedSet<Player>(options);
        const string key = "empty";
        var db = mux.GetDatabase();
        await db.KeyDeleteAsync("sorted:empty");

        // Act
        var items = await sut.RangeByScoreAsync(key, 0, 10, CancellationToken.None);
        var removed = await sut.RemoveAsync(key, new Player("none"), CancellationToken.None);
        mux.Dispose();

        // Assert
        items.Should().BeEmpty();
        removed.Should().BeFalse();
    }
}
