using System.Diagnostics.Metrics;

using Bot.Core.Middlewares;
using Bot.Core.Stats;

using FluentAssertions;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="StatsCollector" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются перцентили и агрегированные метрики.</item>
///         <item>Учитываются отбрасываемые, ограниченные и потерянные при остановке обновления.</item>
///         <item>Экспортируются метрики через <see cref="Meter" />.</item>
///     </list>
/// </remarks>
public class StatsCollectorTests
{
    /// <summary>
    ///     Тест 1: Считаются перцентили, процент ошибок и агрегированные метрики.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Считаются перцентили, процент ошибок и агрегированные метрики")]
    public void Should_CalculatePercentilesAndErrorRate_When_HandlerMeasured()
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
        h.P95.Should().BeGreaterOrEqualTo(h.P50);
        h.P99.Should().BeGreaterOrEqualTo(h.P95);
        h.ErrorRate.Should().BeApproximately(0.5, 0.1);
        snapshot.P95.Should().BeGreaterOrEqualTo(snapshot.P50);
        snapshot.P99.Should().BeGreaterOrEqualTo(snapshot.P95);
        snapshot.ErrorRate.Should().BeApproximately(0.5, 0.1);
        snapshot.Rps.Should().BeGreaterThan(0);
    }

    /// <summary>
    ///     Тест 2: Счётчики отбрасываемых, ограниченных и потерянных при остановке обновлений учитываются.
/// </summary>
[Fact(DisplayName = "Тест 2: Счётчики отбрасываемых, ограниченных и потерянных при остановке обновлений учитываются")]
public void Should_TrackDroppedRateLimitedAndLost_When_Marked()
{
    var stats = new StatsCollector();
    stats.MarkDroppedUpdate("test");
    stats.MarkRateLimited();
    stats.MarkLostUpdates(2);
    stats.SetQueueDepth(5);

    var snapshot = stats.GetSnapshot();
    snapshot.DroppedUpdates.Should().Be(1);
    snapshot.RateLimited.Should().Be(1);
    snapshot.LostUpdates.Should().Be(2);
    snapshot.QueueDepth.Should().Be(5);
}

    /// <summary>
    ///     Тест 3: Метрики экспортируются через Meter.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Метрики экспортируются через Meter")]
    public void Should_ExportMetricsViaMeter_When_MeasurementsRecorded()
    {
        using var meter = new Meter(MetricsMiddleware.MeterName);
        var factory = new TestMeterFactory(meter);
        var stats = new StatsCollector(factory);
        using var listener = new MeterListener();
        var dropped = 0L;
        var rateLimited = 0L;
        var gauge = 0L;
        var latency = 0.0;
        var lost = 0L;

        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == meter)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((inst, value, tags, state) =>
            {
                switch (inst.Name)
                {
                    case "tgbot_dropped_updates_total":
                        dropped = value;
                        break;
                    case "tgbot_rate_limited_total":
                        rateLimited = value;
                        break;
                    case "tgbot_lost_updates_total":
                        lost = value;
                        break;
                    case "queue_depth":
                        gauge = value;
                        break;
                }
            });
        listener.SetMeasurementEventCallback<double>((inst, value, tags, state) =>
        {
            if (inst.Name == "tgbot_handler_latency_ms")
            {
                latency = value;
            }
        });
        listener.Start();

        stats.MarkDroppedUpdate("test");
        stats.MarkRateLimited();
        stats.MarkLostUpdates(2);
        using (var m = stats.Measure("h"))
        {
            Thread.Sleep(1);
        }

        stats.SetQueueDepth(7);
        listener.RecordObservableInstruments();

        dropped.Should().Be(1);
        rateLimited.Should().Be(1);
        lost.Should().Be(2);
        gauge.Should().Be(7);
        latency.Should().BeGreaterThan(0);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly Meter _meter;

        public TestMeterFactory(Meter meter)
        {
            _meter = meter;
        }

        public Meter Create(MeterOptions options)
        {
            return _meter;
        }

        public void Dispose()
        {
        }

        public Meter Create(string name, string? version = null)
        {
            return _meter;
        }
    }
}
