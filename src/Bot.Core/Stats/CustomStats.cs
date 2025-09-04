using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

using Bot.Core.Metrics;
using Bot.Core.Middlewares;

namespace Bot.Core.Stats;

/// <summary>
///     Сборщик пользовательских метрик.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Регистрирует счётчики.</item>
///         <item>Регистрирует гистограммы.</item>
///         <item>Выдаёт снимок значений.</item>
///     </list>
/// </remarks>
public sealed class CustomStats
{
    private readonly ConcurrentDictionary<string, Counter<long>> _counterInstruments = new();
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram<double>> _histogramInstruments = new();
    private readonly ConcurrentDictionary<string, List<double>> _histograms = new();
    private readonly Meter? _meter;

    /// <summary>
    ///     Создать сборщик пользовательских метрик.
    /// </summary>
    /// <param name="meterFactory">Фабрика метрик.</param>
    public CustomStats(IMeterFactory? meterFactory = null)
    {
        if (meterFactory is not null)
        {
            _meter = meterFactory.Create(MetricsMiddleware.MeterName);
        }
    }

    /// <summary>
    ///     Увеличить счётчик.
    /// </summary>
    /// <param name="name">Имя счётчика.</param>
    /// <param name="value">Величина увеличения.</param>
    public void Increment(string name, long value = 1)
    {
        if (_meter is not null)
        {
            var counter = _counterInstruments.GetOrAdd(name, n => _meter.CreateCounter<long>(n, "count"));
            counter.Add(value);
        }

        BotMetricsEventSource.Log.CustomCounter(name, value);
        _counters.AddOrUpdate(name, value, (_, v) => v + value);
    }

    /// <summary>
    ///     Записать значение гистограммы.
    /// </summary>
    /// <param name="name">Имя гистограммы.</param>
    /// <param name="value">Значение.</param>
    public void Record(string name, double value)
    {
        if (_meter is not null)
        {
            var hist = _histogramInstruments.GetOrAdd(name, n => _meter.CreateHistogram<double>(n));
            hist.Record(value);
        }

        BotMetricsEventSource.Log.CustomHistogram(name, value);
        var list = _histograms.GetOrAdd(name, _ => new List<double>());
        lock (list)
        {
            list.Add(value);
        }
    }

    /// <summary>
    ///     Получить снимок пользовательских метрик.
    /// </summary>
    public CustomStatsSnapshot GetSnapshot()
    {
        var counters = new Dictionary<string, long>(_counters);
        var hists = new Dictionary<string, HistogramStat>();
        foreach (var (name, values) in _histograms)
        {
            double[] arr;
            lock (values)
            {
                arr = values.ToArray();
            }

            Array.Sort(arr);
            var p50 = Percentile(arr, 0.50);
            var p95 = Percentile(arr, 0.95);
            var p99 = Percentile(arr, 0.99);
            hists[name] = new HistogramStat(p50, p95, p99);
        }

        return new CustomStatsSnapshot(counters, hists);
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
///     Снимок пользовательских метрик.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Включает значения счётчиков.</item>
///         <item>Включает перцентили гистограмм.</item>
///     </list>
/// </remarks>
public sealed record CustomStatsSnapshot(
    Dictionary<string, long> Counters,
    Dictionary<string, HistogramStat> Histograms);

/// <summary>
///     Перцентили гистограммы.
/// </summary>
public sealed record HistogramStat(double P50, double P95, double P99);
