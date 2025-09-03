using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;

using Bot.Core.Metrics;
using Bot.Core.Middlewares;

using Microsoft.Extensions.Diagnostics;

namespace Bot.Core.Stats;

/// <summary>
/// Сборщик статистики Web App.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>Считает запросы авторизации и профиля.</item>
/// <item>Записывает задержку запросов.</item>
/// </list>
/// </remarks>
public sealed class WebAppStatsCollector
{
    private readonly Counter<long>? _authCounter;
    private readonly Counter<long>? _meCounter;
    private readonly Histogram<double>? _latencyHistogram;
    private readonly List<double> _latencies = new();
    private long _authTotal;
    private long _meTotal;
    private readonly object _sync = new();

    /// <summary>
    /// Создать сборщик статистики Web App.
    /// </summary>
    /// <param name="meterFactory">Фабрика метрик.</param>
    public WebAppStatsCollector(IMeterFactory? meterFactory = null)
    {
        if (meterFactory is not null)
        {
            var meter = meterFactory.Create(MetricsMiddleware.MeterName);
            _authCounter = meter.CreateCounter<long>("tgbot_webapp_auth_total", unit: "count");
            _meCounter = meter.CreateCounter<long>("tgbot_webapp_me_total", unit: "count");
            _latencyHistogram = meter.CreateHistogram<double>("tgbot_webapp_request_latency_ms", unit: "ms");
        }
    }

    /// <summary>
    /// Отметить запрос авторизации.
    /// </summary>
    /// <param name="latencyMs">Длительность запроса в миллисекундах.</param>
    public void MarkAuth(double latencyMs)
    {
        _authCounter?.Add(1);
        _latencyHistogram?.Record(latencyMs);
        BotMetricsEventSource.Log.WebAppAuth(latencyMs);
        Interlocked.Increment(ref _authTotal);
        lock (_sync)
        {
            _latencies.Add(latencyMs);
        }
    }

    /// <summary>
    /// Отметить запрос профиля.
    /// </summary>
    /// <param name="latencyMs">Длительность запроса в миллисекундах.</param>
    public void MarkMe(double latencyMs)
    {
        _meCounter?.Add(1);
        _latencyHistogram?.Record(latencyMs);
        BotMetricsEventSource.Log.WebAppMe(latencyMs);
        Interlocked.Increment(ref _meTotal);
        lock (_sync)
        {
            _latencies.Add(latencyMs);
        }
    }

    /// <summary>
    /// Получить снимок статистики.
    /// </summary>
    public WebAppSnapshot GetSnapshot()
    {
        double[] latencies;
        lock (_sync)
        {
            latencies = _latencies.ToArray();
        }

        Array.Sort(latencies);
        return new WebAppSnapshot(
            Interlocked.Read(ref _authTotal),
            Interlocked.Read(ref _meTotal),
            Percentile(latencies, 0.50),
            Percentile(latencies, 0.95),
            Percentile(latencies, 0.99));
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        var position = percentile * (sorted.Length + 1);
        var index = (int)Math.Floor(position) - 1;
        if (index < 0)
        {
            return sorted[0];
        }

        if (index >= sorted.Length - 1)
        {
            return sorted[^1];
        }

        var fraction = position - Math.Floor(position);
        return sorted[index] + fraction * (sorted[index + 1] - sorted[index]);
    }
}

/// <summary>
/// Снимок статистики Web App.
/// </summary>
/// <remarks>
/// <list type="number">
/// <item>Содержит количества запросов и перцентили задержки.</item>
/// </list>
/// </remarks>
public readonly record struct WebAppSnapshot(long AuthTotal, long MeTotal, double P50, double P95, double P99);

