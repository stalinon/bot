using System;
using System.Collections.Generic;

namespace Bot.Core.Stats;

/// <summary>
///     Вспомогательные данные одного обработчика.
/// </summary>
internal sealed class HandlerData
{
    public long TotalRequests;
    public long ErrorRequests;
    public List<double> Latencies { get; } = new();
    public DateTime StartTime { get; } = DateTime.UtcNow;
    public object SyncRoot { get; } = new();
}

