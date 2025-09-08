using Stalinon.Bot.Core.Stats;

namespace Stalinon.Bot.Admin.MinimalApi;

/// <summary>
///     Проба очереди.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Контролирует глубину очереди обновлений.</item>
///     </list>
/// </remarks>
internal sealed class QueueProbe(StatsCollector stats) : IHealthProbe
{
    private const int Threshold = 1000;

    /// <summary>
    ///     Проверить очередь.
    /// </summary>
    public Task ProbeAsync(CancellationToken ct)
    {
        var depth = stats.GetSnapshot().QueueDepth;
        if (depth > Threshold)
        {
            throw new InvalidOperationException(
                $"глубина очереди {depth} превышает порог {Threshold}");
        }

        return Task.CompletedTask;
    }
}
