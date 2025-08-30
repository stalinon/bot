using System.Collections.Generic;

namespace Bot.Core.Stats;

/// <summary>
///     Снимок текущей статистики.
/// </summary>
public sealed record Snapshot(Dictionary<string, HandlerStat> Handlers);

