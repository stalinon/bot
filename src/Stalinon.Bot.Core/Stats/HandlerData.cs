namespace Stalinon.Bot.Core.Stats;

/// <summary>
///     Вспомогательные данные одного обработчика.
/// </summary>
internal sealed class HandlerData
{
    public long ErrorRequests;
    public long TotalRequests;
    public List<double> Latencies { get; } = new();
    public DateTime StartTime { get; } = DateTime.UtcNow;
    public object SyncRoot { get; } = new();
}
