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
}
