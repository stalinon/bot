using System;
using System.Diagnostics;

namespace Bot.Core.Stats;

/// <summary>
///     Измерение длительности работы обработчика.
/// </summary>
public sealed class Measurement : IDisposable
{
    private readonly HandlerData _info;
    private readonly Stopwatch _sw;
    private readonly StatsCollector _collector;
    private bool _error;

    internal Measurement(HandlerData info, Stopwatch sw, StatsCollector collector)
    {
        _info = info;
        _sw = sw;
        _collector = collector;
    }

    /// <summary>
    ///     Отметить ошибку обработки.
    /// </summary>
    public void MarkError() => _error = true;

    /// <inheritdoc />
    public void Dispose()
    {
        _sw.Stop();
        _collector.RecordHandler(_sw.Elapsed.TotalMilliseconds, !_error);
        lock (_info.SyncRoot)
        {
            _info.Latencies.Add(_sw.Elapsed.TotalMilliseconds);
            if (_error)
            {
                _info.ErrorRequests++;
            }
        }
    }
}

