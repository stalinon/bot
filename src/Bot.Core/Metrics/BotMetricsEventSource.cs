using System.Collections.Concurrent;
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

    private readonly ConcurrentDictionary<string, IncrementingEventCounter> _customCounters = new();
    private readonly ConcurrentDictionary<string, EventCounter> _customHistograms = new();
    private readonly IncrementingEventCounter _dropped;
    private readonly IncrementingEventCounter _errors;
    private readonly EventCounter _handlerLatency;
    private readonly EventCounter _queueDepth;
    private readonly IncrementingEventCounter _rateLimited;
    private readonly EventCounter _updateLatency;

    private readonly IncrementingEventCounter _updates;
    private readonly IncrementingEventCounter _webAppAuth;
    private readonly EventCounter _webAppLatency;
    private readonly IncrementingEventCounter _webAppMe;
    private readonly IncrementingEventCounter _webAppSendData;

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
        _webAppAuth = new IncrementingEventCounter("tgbot_webapp_auth_total", this)
        {
            DisplayName = "Авторизация Web App"
        };
        _webAppMe = new IncrementingEventCounter("tgbot_webapp_me_total", this)
        {
            DisplayName = "Профиль Web App"
        };
        _webAppSendData = new IncrementingEventCounter("tgbot_webapp_senddata_total", this)
        {
            DisplayName = "Передача данных Web App"
        };
        _webAppLatency = new EventCounter("tgbot_webapp_request_latency_ms", this)
        {
            DisplayName = "Задержка запроса Web App, мс"
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
    ///     Отметить запрос авторизации Web App.
    /// </summary>
    /// <param name="latencyMs">Задержка запроса в миллисекундах.</param>
    public void WebAppAuth(double latencyMs)
    {
        _webAppAuth.Increment();
        _webAppLatency.WriteMetric(latencyMs);
    }

    /// <summary>
    ///     Отметить запрос профиля Web App.
    /// </summary>
    /// <param name="latencyMs">Задержка запроса в миллисекундах.</param>
    public void WebAppMe(double latencyMs)
    {
        _webAppMe.Increment();
        _webAppLatency.WriteMetric(latencyMs);
    }

    /// <summary>
    ///     Отметить получение данных Web App.
    /// </summary>
    /// <param name="latencyMs">Задержка обработки в миллисекундах.</param>
    public void WebAppSendData(double latencyMs)
    {
        _webAppSendData.Increment();
        _webAppLatency.WriteMetric(latencyMs);
    }

    /// <summary>
    ///     Увеличить пользовательский счётчик.
    /// </summary>
    /// <param name="name">Имя счётчика.</param>
    /// <param name="value">Величина увеличения.</param>
    public void CustomCounter(string name, long value)
    {
        var counter = _customCounters.GetOrAdd(name, n =>
            new IncrementingEventCounter(n, this) { DisplayName = name });
        counter.Increment(value);
    }

    /// <summary>
    ///     Записать значение пользовательской гистограммы.
    /// </summary>
    /// <param name="name">Имя гистограммы.</param>
    /// <param name="value">Значение.</param>
    public void CustomHistogram(string name, double value)
    {
        var hist = _customHistograms.GetOrAdd(name, n => new EventCounter(n, this)
        {
            DisplayName = name
        });
        hist.WriteMetric(value);
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
            _webAppAuth.Dispose();
            _webAppMe.Dispose();
            _webAppSendData.Dispose();
            _webAppLatency.Dispose();
            foreach (var c in _customCounters.Values)
            {
                c.Dispose();
            }

            foreach (var h in _customHistograms.Values)
            {
                h.Dispose();
            }
        }

        base.Dispose(disposing);
    }
}
