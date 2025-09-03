using System.Diagnostics.Tracing;

namespace Bot.Core.Metrics;

/// <summary>
///     Источник EventCounters для метрик бота.
/// </summary>
[EventSource(Name = "Bot.Core")]
public sealed class BotMetricsEventSource : EventSource
{
    /// <summary>
    ///     Единственный экземпляр источника.
    /// </summary>
    public static readonly BotMetricsEventSource Log = new();

    private readonly IncrementingEventCounter _updates;
    private readonly IncrementingEventCounter _errors;
    private readonly IncrementingEventCounter _dropped;
    private readonly IncrementingEventCounter _rateLimited;
    private readonly EventCounter _updateLatency;
    private readonly EventCounter _handlerLatency;
    private readonly EventCounter _queueDepth;

    private BotMetricsEventSource()
    {
        _updates = new IncrementingEventCounter("tgbot_updates_total", this)
        {
            DisplayName = "Обновления"
        };
        _errors = new IncrementingEventCounter("tgbot_errors_total", this)
        {
            DisplayName = "Ошибки"
        };
        _dropped = new IncrementingEventCounter("tgbot_dropped_updates_total", this)
        {
            DisplayName = "Потерянные обновления"
        };
        _rateLimited = new IncrementingEventCounter("tgbot_rate_limited_total", this)
        {
            DisplayName = "Ограниченные обновления"
        };
        _updateLatency = new EventCounter("tgbot_update_latency_ms", this)
        {
            DisplayName = "Задержка обновления, мс"
        };
        _handlerLatency = new EventCounter("tgbot_handler_latency_ms", this)
        {
            DisplayName = "Задержка обработчика, мс"
        };
        _queueDepth = new EventCounter("tgbot_queue_depth", this)
        {
            DisplayName = "Глубина очереди"
        };
    }

    /// <summary>
    ///     Записать результат обработки обновления.
    /// </summary>
    /// <param name="latencyMs">Задержка обработки обновления в миллисекундах.</param>
    /// <param name="success">Признак успешной обработки.</param>
    public void Update(double latencyMs, bool success)
    {
        _updates.Increment();
        if (!success)
        {
            _errors.Increment();
        }

        _updateLatency.WriteMetric(latencyMs);
    }

    /// <summary>
    ///     Записать результат работы обработчика.
    /// </summary>
    /// <param name="latencyMs">Задержка обработчика в миллисекундах.</param>
    /// <param name="success">Признак успешной обработки.</param>
    public void Handler(double latencyMs, bool success)
    {
        if (!success)
        {
            _errors.Increment();
        }

        _handlerLatency.WriteMetric(latencyMs);
    }

    /// <summary>
    ///     Увеличить счётчик потерянных обновлений.
    /// </summary>
    public void MarkDroppedUpdate()
    {
        _dropped.Increment();
    }

    /// <summary>
    ///     Увеличить счётчик ограниченных обновлений.
    /// </summary>
    public void MarkRateLimited()
    {
        _rateLimited.Increment();
    }

    /// <summary>
    ///     Установить текущую глубину очереди.
    /// </summary>
    public void SetQueueDepth(long depth)
    {
        _queueDepth.WriteMetric(depth);
    }

    /// <summary>
    ///     Освободить ресурсы.
    /// </summary>
    /// <param name="disposing">Признак явной очистки.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updates.Dispose();
            _errors.Dispose();
            _dropped.Dispose();
            _rateLimited.Dispose();
            _updateLatency.Dispose();
            _handlerLatency.Dispose();
            _queueDepth.Dispose();
        }

        base.Dispose(disposing);
    }
}

