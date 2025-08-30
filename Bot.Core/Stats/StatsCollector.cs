using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Bot.Core.Stats;

/// <summary>
///     Сборщик статистики по обработчикам.
/// </summary>
public sealed class StatsCollector
{
    private readonly ConcurrentDictionary<string, HandlerData> _data = new();

    /// <summary>
    ///     Начать измерение для обработчика.
    /// </summary>
    public Measurement Measure(string handler)
    {
        var info = _data.GetOrAdd(handler, _ => new HandlerData());
        Interlocked.Increment(ref info.TotalRequests);
        return new Measurement(info, Stopwatch.StartNew());
    }

    /// <summary>
    ///     Получить текущую статистику.
    /// </summary>
    public Snapshot GetSnapshot()
    {
        var dict = new Dictionary<string, HandlerStat>();
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
            }
        }

        return new Snapshot(dict);
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

