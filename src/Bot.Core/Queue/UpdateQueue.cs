using System.Threading.Channels;

using Bot.Core.Options;
using Bot.Core.Stats;

namespace Bot.Core.Queue;

/// <summary>
///     Очередь с ограниченной ёмкостью.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Поддерживает отбрасывание или ожидание при переполнении.</item>
///         <item>Обновляет метрики глубины и потерь.</item>
///     </list>
/// </remarks>
public sealed class UpdateQueue<T>
{
    private readonly Channel<T> _channel;
    private readonly QueuePolicy _policy;
    private readonly StatsCollector _stats;

    /// <summary>
    ///     Создать очередь.
    /// </summary>
    public UpdateQueue(int capacity, QueuePolicy policy, StatsCollector stats)
    {
        _policy = policy;
        _stats = stats;
        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    /// <summary>
    ///     Текущий размер очереди.
    /// </summary>
    public int Count => _channel.Reader.Count;

    /// <summary>
    ///     Прочитать все элементы очереди.
    /// </summary>
    public IAsyncEnumerable<T> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);

    /// <summary>
    ///     Добавить элемент в очередь.
    /// </summary>
    /// <returns><c>true</c>, если элемент помещён.</returns>
    public async ValueTask<bool> EnqueueAsync(T item, CancellationToken ct)
    {
        if (_policy == QueuePolicy.Wait)
        {
            await _channel.Writer.WriteAsync(item, ct);
            _stats.SetQueueDepth(_channel.Reader.Count);
            return true;
        }

        var written = _channel.Writer.TryWrite(item);
        _stats.SetQueueDepth(_channel.Reader.Count);
        if (!written)
        {
            _stats.MarkDroppedUpdate(_policy.ToString().ToLowerInvariant());
        }

        return written;
    }

    /// <summary>
    ///     Завершить очередь.
    /// </summary>
    public void Complete() => _channel.Writer.TryComplete();
}
