using System.Diagnostics.Metrics;
using System.Threading;
using Bot.Core.Middlewares;
using Bot.Core.Stats;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics;
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
        h.P95.Should().BeGreaterOrEqualTo(h.P50);
        h.P99.Should().BeGreaterOrEqualTo(h.P95);
        h.ErrorRate.Should().BeApproximately(0.5, 0.1);
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
        snapshot.DroppedUpdates.Should().Be(1);
        snapshot.RateLimited.Should().Be(1);
        snapshot.QueueDepth.Should().Be(5);
    }

    /// <summary>
    ///     Тест 3. Метрики экспортируются через Meter.
    /// </summary>
    [Fact(DisplayName = "Тест 3. Метрики экспортируются через Meter")]
    public void Exports_metrics_via_meter()
    {
        using var meter = new Meter(MetricsMiddleware.MeterName);
        var factory = new TestMeterFactory(meter);
        var stats = new StatsCollector(factory);
        using var listener = new MeterListener();
        var dropped = 0L;
        var rateLimited = 0L;
        var gauge = 0L;
        var latency = 0.0;

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
                case "tgbot_queue_depth":
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

        stats.MarkDroppedUpdate();
        stats.MarkRateLimited();
        using (var m = stats.Measure("h"))
        {
            Thread.Sleep(1);
        }
        stats.SetQueueDepth(7);
        listener.RecordObservableInstruments();

        dropped.Should().Be(1);
        rateLimited.Should().Be(1);
        gauge.Should().Be(7);
        latency.Should().BeGreaterThan(0);
    }

    private sealed class TestMeterFactory : IMeterFactory
    {
        private readonly Meter _meter;

        public TestMeterFactory(Meter meter) => _meter = meter;

        public Meter Create(string name, string? version = null) => _meter;

        public Meter Create(MeterOptions options) => _meter;

        public void Dispose()
        {
        }
    }
}
