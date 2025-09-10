using System;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Moq;

using StackExchange.Redis;

using Stalinon.Bot.Outbox;

using Xunit;

namespace Stalinon.Bot.Outbox.Tests;

/// <summary>
///     Тесты для <see cref="RedisOutbox" />: добавление, чтение и подтверждение сообщений
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Добавляет и подтверждает сообщение при успешном транспорте.</item>
///         <item>Читает количество ожидающих сообщений.</item>
///         <item>Пробрасывает исключение при недоступности Redis.</item>
///     </list>
/// </remarks>
public sealed class RedisOutboxTests
{
    /// <inheritdoc />
    public RedisOutboxTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен добавлять и подтверждать сообщение при успешном транспорте
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен добавлять и подтверждать сообщение при успешном транспорте")]
    public async Task Should_AddAndConfirm_WhenTransportSucceeds()
    {
        // Arrange
        var db = new Mock<IDatabase>();
        db.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);
        db.Setup(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        var outbox = new RedisOutbox(db.Object);

        // Act
        await outbox.SendAsync("1", "payload", (_, _, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        db.Verify(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()), Times.Once);
        db.Verify(x => x.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    /// <summary>
    ///     Тест 2: Должен читать количество ожидающих сообщений
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен читать количество ожидающих сообщений")]
    public async Task Should_ReadPending_WhenMessagesExist()
    {
        // Arrange
        var db = new Mock<IDatabase>();
        db.SetupGet(x => x.Database).Returns(0);
        var muxer = new Mock<IConnectionMultiplexer>();
        db.SetupGet(x => x.Multiplexer).Returns(muxer.Object);
        var server = new Mock<IServer>();
        muxer.Setup(x => x.GetServers()).Returns(new[] { server.Object });
        server.Setup(x => x.Keys(0, It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(new[] { (RedisKey)"outbox:1", (RedisKey)"outbox:2" });
        var outbox = new RedisOutbox(db.Object);

        // Act
        var pending = await outbox.GetPendingAsync(CancellationToken.None);

        // Assert
        pending.Should().Be(2);
        server.Verify(x => x.Keys(0, It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    /// <summary>
    ///     Тест 3: Должен пробрасывать исключение при недоступности Redis
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен пробрасывать исключение при недоступности Redis")]
    public async Task Should_Throw_WhenRedisUnavailable()
    {
        // Arrange
        var db = new Mock<IDatabase>();
        db.Setup(x => x.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToResolvePhysicalConnection, "нет соединения"));
        var outbox = new RedisOutbox(db.Object);

        // Act
        var act = () => outbox.SendAsync("1", "payload", (_, _, _) => Task.CompletedTask, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<RedisConnectionException>();
    }
}

