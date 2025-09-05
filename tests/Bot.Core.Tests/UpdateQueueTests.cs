using Bot.Core.Options;
using Bot.Core.Queue;
using Bot.Core.Stats;

using FluentAssertions;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты очереди обновлений.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется ожидание при политике Wait.</item>
///         <item>Проверяется отбрасывание при политике Drop.</item>
///     </list>
/// </remarks>
public sealed class UpdateQueueTests
{
    /// <summary>
    ///     Тест 1: Политика Wait ожидает освобождения места.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Политика Wait ожидает освобождения места.")]
    public async Task Should_Wait_When_PolicyIsWait()
    {
        var stats = new StatsCollector();
        var queue = new UpdateQueue<int>(1, QueuePolicy.Wait, stats);
        await queue.EnqueueAsync(1, CancellationToken.None);
        var cts = new CancellationTokenSource();
        var second = queue.EnqueueAsync(2, cts.Token).AsTask();
        await Task.Delay(50);
        second.IsCompleted.Should().BeFalse();

        var readTask = Task.Run(async () =>
        {
            await foreach (var item in queue.ReadAllAsync(cts.Token))
            {
                return item;
            }

            return -1;
        });

        var first = await readTask;
        first.Should().Be(1);
        await Task.Delay(50);
        second.IsCompleted.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 2: Политика Drop отбрасывает при переполнении.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Политика Drop отбрасывает при переполнении.")]
    public async Task Should_Drop_When_PolicyIsDrop()
    {
        var stats = new StatsCollector();
        var queue = new UpdateQueue<int>(1, QueuePolicy.Drop, stats);
        var accepted = await queue.EnqueueAsync(1, CancellationToken.None);
        accepted.Should().BeTrue();
        var dropped = await queue.EnqueueAsync(2, CancellationToken.None);
        dropped.Should().BeFalse();
        var snapshot = stats.GetSnapshot();
        snapshot.DroppedUpdates.Should().Be(1);
        snapshot.QueueDepth.Should().Be(1);
    }
}
