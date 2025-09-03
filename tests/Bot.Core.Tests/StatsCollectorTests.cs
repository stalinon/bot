using System.Threading;
using Bot.Core.Stats;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="StatsCollector"/>.
/// </summary>
public class StatsCollectorTests
{
    /// <summary>
    ///     Считаются перцентили и процент ошибок.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Считаются перцентили и процент ошибок")]
    public void Calculates_percentiles_and_error_rate()
    {
        var stats = new StatsCollector();
        for (var i = 0; i < 10; i++)
        {
            using var m = stats.Measure("handler");
            Thread.Sleep(1);
            if (i % 2 == 0)
            {
                m.MarkError();
            }
        }

        var snapshot = stats.GetSnapshot();
        var h = snapshot.Handlers["handler"];
        Assert.True(h.P95 >= h.P50);
        Assert.True(h.P99 >= h.P95);
        Assert.Equal(0.5, h.ErrorRate, 1);
    }

    /// <summary>
    ///     Тест 2. Счётчики потерянных и ограниченных обновлений учитываются
    /// </summary>
    [Fact(DisplayName = "Тест 2. Счётчики потерянных и ограниченных обновлений учитываются")]
    public void Tracks_dropped_and_rate_limited()
    {
        var stats = new StatsCollector();
        stats.MarkDroppedUpdate();
        stats.MarkRateLimited();
        stats.SetQueueDepth(5);

        var snapshot = stats.GetSnapshot();
        Assert.Equal(1, snapshot.DroppedUpdates);
        Assert.Equal(1, snapshot.RateLimited);
        Assert.Equal(5, snapshot.QueueDepth);
    }
}
