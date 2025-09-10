using System.Diagnostics;

using FluentAssertions;

using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Core.Tests.Stats;

/// <summary>
///     Тесты жизненного цикла <see cref="StatsCollector" />: накопление, сброс и агрегация метрик.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется накопление метрик нескольких обработчиков.</item>
///         <item>Проверяется агрегация метрик по всем обработчикам.</item>
///         <item>Проверяется сброс метрик при создании нового экземпляра.</item>
///     </list>
/// </remarks>
public sealed class StatsCollectorLifecycleTests
{
    /// <inheritdoc/>
    public StatsCollectorLifecycleTests()
    {
    }

    /// <summary>
    ///     Тест 1: Снимок содержит корректные метрики по обработчикам и суммарно.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Снимок содержит корректные метрики по обработчикам и суммарно")]
    public void Should_AccumulateAndAggregate_When_MultipleHandlersMeasured()
    {
        var stats = new StatsCollector();
        var h1Latencies = new List<double>();
        var h2Latencies = new List<double>();

        using (var m = stats.Measure("h1"))
        {
            var sw = Stopwatch.StartNew();
            Thread.Sleep(10);
            sw.Stop();
            h1Latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        using (var m = stats.Measure("h1"))
        {
            var sw = Stopwatch.StartNew();
            Thread.Sleep(20);
            sw.Stop();
            h1Latencies.Add(sw.Elapsed.TotalMilliseconds);
            m.MarkError();
        }

        using (var m = stats.Measure("h2"))
        {
            var sw = Stopwatch.StartNew();
            Thread.Sleep(5);
            sw.Stop();
            h2Latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        using (var m = stats.Measure("h2"))
        {
            var sw = Stopwatch.StartNew();
            Thread.Sleep(15);
            sw.Stop();
            h2Latencies.Add(sw.Elapsed.TotalMilliseconds);
        }

        var snapshot = stats.GetSnapshot();
        var h1 = snapshot.Handlers["h1"];
        var h2 = snapshot.Handlers["h2"];
        var expectedH1 = CalcExpected(h1Latencies.ToArray());
        var expectedH2 = CalcExpected(h2Latencies.ToArray());
        var expectedGlobal = CalcExpected(h1Latencies.Concat(h2Latencies).ToArray());

        h1.P50.Should().BeApproximately(expectedH1.P50, 5);
        h1.P95.Should().BeApproximately(expectedH1.P95, 5);
        h1.P99.Should().BeApproximately(expectedH1.P99, 5);
        h1.ErrorRate.Should().BeApproximately(0.5, 0.01);
        h1.Rps.Should().BeApproximately(expectedH1.Rps, 0.1);

        h2.P50.Should().BeApproximately(expectedH2.P50, 5);
        h2.P95.Should().BeApproximately(expectedH2.P95, 5);
        h2.P99.Should().BeApproximately(expectedH2.P99, 5);
        h2.ErrorRate.Should().BeApproximately(0, 0.01);
        h2.Rps.Should().BeApproximately(expectedH2.Rps, 0.1);

        snapshot.P50.Should().BeApproximately(expectedGlobal.P50, 5);
        snapshot.P95.Should().BeApproximately(expectedGlobal.P95, 5);
        snapshot.P99.Should().BeApproximately(expectedGlobal.P99, 5);
        snapshot.ErrorRate.Should().BeApproximately(0.25, 0.01);
        snapshot.Rps.Should().BeApproximately(expectedGlobal.Rps, 0.1);
    }

    /// <summary>
    ///     Тест 2: Метрики сбрасываются при создании нового экземпляра.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Метрики сбрасываются при создании нового экземпляра")]
    public void Should_ResetMetrics_When_NewInstanceCreated()
    {
        var stats = new StatsCollector();
        using (stats.Measure("h"))
        {
            Thread.Sleep(1);
        }

        stats.GetSnapshot().Handlers.Should().ContainKey("h");

        stats = new StatsCollector();
        var snapshot = stats.GetSnapshot();
        snapshot.Handlers.Should().BeEmpty();
        snapshot.DroppedUpdates.Should().Be(0);
        snapshot.RateLimited.Should().Be(0);
        snapshot.LostUpdates.Should().Be(0);
    }

    private static HandlerStat CalcExpected(double[] latencies)
    {
        Array.Sort(latencies);
        var p50 = Percentile(latencies, 0.50);
        var p95 = Percentile(latencies, 0.95);
        var p99 = Percentile(latencies, 0.99);
        var rps = latencies.Length; // время меньше секунды
        return new HandlerStat(p50, p95, p99, rps, 0);
    }

    private static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        var position = percentile * (sorted.Length + 1);
        var index = (int)Math.Floor(position) - 1;
        if (index < 0)
        {
            return sorted[0];
        }

        if (index >= sorted.Length - 1)
        {
            return sorted[^1];
        }

        var fraction = position - Math.Floor(position);
        return sorted[index] + fraction * (sorted[index + 1] - sorted[index]);
    }
}

