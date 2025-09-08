namespace Stalinon.Bot.Core.Stats;

/// <summary>
///     Снимок текущей статистики.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Включает метрики обработчиков.</item>
///         <item>Содержит агрегированные метрики.</item>
///         <item>Содержит счётчики отбрасываемых, ограниченных и потерянных при остановке обновлений.</item>
///         <item>Хранит текущую глубину очереди.</item>
///     </list>
/// </remarks>
public sealed record Snapshot(
    Dictionary<string, HandlerStat> Handlers,
    double P50,
    double P95,
    double P99,
    double Rps,
    double ErrorRate,
    long DroppedUpdates,
    long RateLimited,
    long LostUpdates,
    int QueueDepth);
