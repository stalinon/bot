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
    private readonly EventCounter _latency;

    private BotMetricsEventSource()
    {
        _updates = new IncrementingEventCounter("updates", this)
        {
            DisplayName = "Обновления"
        };
        _errors = new IncrementingEventCounter("errors", this)
        {
            DisplayName = "Ошибки"
        };
        _latency = new EventCounter("latency", this)
        {
            DisplayName = "Задержка, мс"
        };
    }

    /// <summary>
    ///     Записать результат обработки обновления.
    /// </summary>
    /// <param name="latencyMs">Задержка обработки в миллисекундах.</param>
    /// <param name="success">Признак успешной обработки.</param>
    public void Update(double latencyMs, bool success)
    {
        _updates.Increment();
        if (!success)
        {
            _errors.Increment();
        }

        _latency.WriteMetric(latencyMs);
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
            _latency.Dispose();
        }

        base.Dispose(disposing);
    }
}

