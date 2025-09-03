using System.Collections.Generic;

namespace Bot.Core.Stats;

/// <summary>
///     Снимок текущей статистики.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Включает метрики обработчиков.</item>
///         <item>Содержит счётчики потерянных и ограниченных обновлений.</item>
///         <item>Хранит текущую глубину очереди.</item>
///     </list>
/// </remarks>
public sealed record Snapshot(
    Dictionary<string, HandlerStat> Handlers,
    long DroppedUpdates,
    long RateLimited,
    int QueueDepth);

