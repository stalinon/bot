using FluentAssertions;

using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Queue;
using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты базовых операций очереди.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется добавление в очередь.</item>
///         <item>Проверяется извлечение из очереди.</item>
///         <item>Проверяется блокировка при переполнении.</item>
///         <item>Проверяется отмена ожидания при добавлении.</item>
///     </list>
/// </remarks>
public sealed class UpdateQueueOperationsTests
{
    /// <summary>
    ///     Тест 1: Должен помещать элемент в очередь.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен помещать элемент в очередь.")]
    public async Task Should_EnqueueItem_When_CapacityAvailable()
    {
        var stats = new StatsCollector();
        var queue = new UpdateQueue<int>(1, QueuePolicy.Wait, stats);

        var accepted = await queue.EnqueueAsync(42, CancellationToken.None);

        accepted.Should().BeTrue();
        queue.Count.Should().Be(1);
    }

    /// <summary>
    ///     Тест 2: Должен извлекать ранее добавленный элемент.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен извлекать ранее добавленный элемент.")]
    public async Task Should_ReturnItem_When_Read()
    {
        var stats = new StatsCollector();
        var queue = new UpdateQueue<int>(1, QueuePolicy.Wait, stats);

        await queue.EnqueueAsync(7, CancellationToken.None);

        await foreach (var item in queue.ReadAllAsync(CancellationToken.None))
        {
            item.Should().Be(7);
            break;
        }
    }

    /// <summary>
    ///     Тест 3: Должен блокироваться при переполнении очереди.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен блокироваться при переполнении очереди.")]
    public async Task Should_Block_When_QueueIsFull()
    {
        var stats = new StatsCollector();
        var queue = new UpdateQueue<int>(1, QueuePolicy.Wait, stats);
        await queue.EnqueueAsync(1, CancellationToken.None);

        var cts = new CancellationTokenSource();
        var second = queue.EnqueueAsync(2, cts.Token).AsTask();
        await Task.Delay(50);
        second.IsCompleted.Should().BeFalse();

        _ = Task.Run(async () =>
        {
            await foreach (var _ in queue.ReadAllAsync(cts.Token))
            {
                break;
            }
        });

        await Task.Delay(50);
        second.IsCompleted.Should().BeTrue();
    }

    /// <summary>
    ///     Тест 4: Должен отменять ожидание при отмене токена.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен отменять ожидание при отмене токена.")]
    public async Task Should_Throw_When_CancelledDuringEnqueue()
    {
        var stats = new StatsCollector();
        var queue = new UpdateQueue<int>(1, QueuePolicy.Wait, stats);
        await queue.EnqueueAsync(1, CancellationToken.None);

        var cts = new CancellationTokenSource();
        var action = async () => await queue.EnqueueAsync(2, cts.Token);
        await Task.Delay(50);
        cts.Cancel();

        await action.Should().ThrowAsync<OperationCanceledException>();
    }
}

