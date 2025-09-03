using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;

using Bot.Core.Metrics;
using Bot.Core.Middlewares;

using Microsoft.Extensions.Diagnostics;

namespace Bot.Core.Stats;

/// <summary>
///     Сборщик статистики по обработчикам.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Сохраняет метрики обработчиков.</item>
///         <item>Учитывает потерянные и ограниченные обновления.</item>
///         <item>Следит за глубиной очереди.</item>
///     </list>
/// </remarks>
public sealed class StatsCollector
{
    private readonly ConcurrentDictionary<string, HandlerData> _data = new();
    private readonly Counter<long>? _droppedCounter;
    private readonly Counter<long>? _rateLimitedCounter;
    private readonly Histogram<double>? _handlerLatency;
    private readonly ObservableGauge<long>? _queueGauge;
    private readonly DateTime _startTime = DateTime.UtcNow;
    private int _queueDepth;
    private long _droppedUpdates;
    private long _rateLimited;

    /// <summary>
    ///     Создать сборщик статистики.
    /// </summary>
    /// <param name="meterFactory">Фабрика метрик.</param>
    public StatsCollector(IMeterFactory? meterFactory = null)
    {
        if (meterFactory is not null)
        {
            var meter = meterFactory.Create(MetricsMiddleware.MeterName);
            _droppedCounter = meter.CreateCounter<long>("tgbot_dropped_updates_total", unit: "count");
            _rateLimitedCounter = meter.CreateCounter<long>("tgbot_rate_limited_total", unit: "count");
            _handlerLatency = meter.CreateHistogram<double>("tgbot_handler_latency_ms", unit: "ms");
            _queueGauge = meter.CreateObservableGauge<long>("tgbot_queue_depth", () => Volatile.Read(ref _queueDepth));
        }
    }

    /// <summary>
    ///     Начать измерение для обработчика.
    /// </summary>
    public Measurement Measure(string handler)
    {
        var info = _data.GetOrAdd(handler, _ => new HandlerData());
        Interlocked.Increment(ref info.TotalRequests);
        return new Measurement(info, Stopwatch.StartNew(), this);
    }

    /// <summary>
    ///     Записать метрику обработчика.
    /// </summary>
    /// <param name="latencyMs">Длительность обработчика в миллисекундах.</param>
    /// <param name="success">Признак успешной обработки.</param>
    internal void RecordHandler(double latencyMs, bool success)
    {
        _handlerLatency?.Record(latencyMs);
        BotMetricsEventSource.Log.Handler(latencyMs, success);
    }

    /// <summary>
    ///     Увеличить счётчик потерянных обновлений.
    /// </summary>
    public void MarkDroppedUpdate()
    {
        _droppedCounter?.Add(1);
        BotMetricsEventSource.Log.MarkDroppedUpdate();
        Interlocked.Increment(ref _droppedUpdates);
    }

    /// <summary>
    ///     Увеличить счётчик ограниченных обновлений.
    /// </summary>
    public void MarkRateLimited()
    {
        _rateLimitedCounter?.Add(1);
        BotMetricsEventSource.Log.MarkRateLimited();
        Interlocked.Increment(ref _rateLimited);
    }

    /// <summary>
    ///     Установить текущую глубину очереди.
    /// </summary>
    public void SetQueueDepth(int depth)
    {
        Volatile.Write(ref _queueDepth, depth);
        BotMetricsEventSource.Log.SetQueueDepth(depth);
    }

    /// <summary>
    ///     Получить текущую статистику.
    /// </summary>
    public Snapshot GetSnapshot()
    {
        var dict = new Dictionary<string, HandlerStat>();
        var allLatencies = new List<double>();
        long totalRequests = 0;
        long errorRequests = 0;

        foreach (var (name, data) in _data)
        {
            lock (data.SyncRoot)
            {
                var latencies = data.Latencies.ToArray();
                Array.Sort(latencies);
                var p50 = Percentile(latencies, 0.50);
                var p95 = Percentile(latencies, 0.95);
                var p99 = Percentile(latencies, 0.99);
                var seconds = Math.Max(1, (DateTime.UtcNow - data.StartTime).TotalSeconds);
                var rps = data.TotalRequests / seconds;
                var errorRate = data.TotalRequests == 0 ? 0 : (double)data.ErrorRequests / data.TotalRequests;
                dict[name] = new HandlerStat(p50, p95, p99, rps, errorRate);

                allLatencies.AddRange(latencies);
                totalRequests += data.TotalRequests;
                errorRequests += data.ErrorRequests;
            }
        }

        var globalLatencies = allLatencies.ToArray();
        Array.Sort(globalLatencies);
        var totalP50 = Percentile(globalLatencies, 0.50);
        var totalP95 = Percentile(globalLatencies, 0.95);
        var totalP99 = Percentile(globalLatencies, 0.99);
        var totalSeconds = Math.Max(1, (DateTime.UtcNow - _startTime).TotalSeconds);
        var totalRps = totalRequests / totalSeconds;
        var totalErrorRate = totalRequests == 0 ? 0 : (double)errorRequests / totalRequests;

        return new Snapshot(
            dict,
            totalP50,
            totalP95,
            totalP99,
            totalRps,
            totalErrorRate,
            Interlocked.Read(ref _droppedUpdates),
            Interlocked.Read(ref _rateLimited),
            Volatile.Read(ref _queueDepth));
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0) return 0;
        var position = percentile * (sorted.Length + 1);
        var index = (int)Math.Floor(position) - 1;
        if (index < 0) return sorted[0];
        if (index >= sorted.Length - 1) return sorted[^1];
        var fraction = position - Math.Floor(position);
        return sorted[index] + fraction * (sorted[index + 1] - sorted[index]);
    }

}

