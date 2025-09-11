using FluentAssertions;

using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.TestKit.Tests;

/// <summary>
///     Тесты FakeDistributedLock: проверка захвата, освобождения и поведения при отмене.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется захват и освобождение вручную</item>
///         <item>Проверяется отказ при повторном захвате</item>
///         <item>Проверяется освобождение по TTL</item>
///         <item>Проверяется отмена операции</item>
///     </list>
/// </remarks>
public sealed class FakeDistributedLockTests
{
    /// <inheritdoc/>
    public FakeDistributedLockTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен захватывать и освобождать лок вручную.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен захватывать и освобождать лок вручную")]
    public async Task Should_AcquireAndReleaseLock()
    {
        // Arrange
        var @lock = new FakeDistributedLock();

        // Act
        var first = await @lock.AcquireAsync("key", TimeSpan.FromSeconds(1), CancellationToken.None);
        await @lock.ReleaseAsync("key", CancellationToken.None);
        var second = await @lock.AcquireAsync("key", TimeSpan.FromSeconds(1), CancellationToken.None);

        // Assert
        first.Should().BeTrue();
        second.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 2: Должен отказывать в захвате активного лока.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен отказывать в захвате активного лока")]
    public async Task Should_Reject_WhenLockHeld()
    {
        // Arrange
        var @lock = new FakeDistributedLock();

        // Act
        var first = await @lock.AcquireAsync("key", TimeSpan.FromSeconds(1), CancellationToken.None);
        var second = await @lock.AcquireAsync("key", TimeSpan.FromSeconds(1), CancellationToken.None);

        // Assert
        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    /// <summary>
    ///     Тест 3: Лок должен освобождаться по TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Лок должен освобождаться по TTL")]
    public async Task Should_ReleaseLock_ByTtl()
    {
        // Arrange
        var @lock = new FakeDistributedLock();
        await @lock.AcquireAsync("key", TimeSpan.FromMilliseconds(20), CancellationToken.None);

        // Act
        await Task.Delay(40);
        var second = await @lock.AcquireAsync("key", TimeSpan.FromMilliseconds(20), CancellationToken.None);

        // Assert
        second.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 4: Должен бросать исключение при отмене операции.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен бросать исключение при отмене операции")]
    public async Task Should_Throw_WhenCancelled()
    {
        // Arrange
        var @lock = new FakeDistributedLock();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await @lock.AcquireAsync("key", TimeSpan.FromSeconds(1), cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}

