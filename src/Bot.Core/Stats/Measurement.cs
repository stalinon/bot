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
    private bool _error;

    internal Measurement(HandlerData info, Stopwatch sw)
    {
        _info = info;
        _sw = sw;
    }

    /// <summary>
    ///     Отметить ошибку обработки.
    /// </summary>
    public void MarkError() => _error = true;

    /// <inheritdoc />
    public void Dispose()
    {
        _sw.Stop();
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

