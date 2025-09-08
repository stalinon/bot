namespace Stalinon.Bot.Core.Stats;

/// <summary>
///     Метрика одного обработчика.
/// </summary>
public sealed record HandlerStat(double P50, double P95, double P99, double Rps, double ErrorRate);
