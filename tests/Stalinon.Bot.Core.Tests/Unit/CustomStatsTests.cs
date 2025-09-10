using System.Diagnostics.Metrics;

using FluentAssertions;

using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="CustomStats" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется регистрация и снимок пользовательских метрик.</item>
///         <item>Проверяется экспорт через <see cref="Meter" />.</item>
///     </list>
/// </remarks>
public sealed class CustomStatsTests
{
    /// <summary>
    ///     Тест 1: Счётчик и гистограмма возвращаются в снимке.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Счётчик и гистограмма возвращаются в снимке")]
    public void Should_ReturnCounterAndHistogram_When_Recorded()
    {
        var stats = new CustomStats();
        stats.Increment("counter");
        stats.Record("hist", 1);
        stats.Record("hist", 3);

        var snapshot = stats.GetSnapshot();
        snapshot.Counters.Should().ContainKey("counter").WhoseValue.Should().Be(1);
        snapshot.Histograms.Should().ContainKey("hist");
        var hist = snapshot.Histograms["hist"];
        hist.P95.Should().BeGreaterOrEqualTo(hist.P50);
        hist.P99.Should().BeGreaterOrEqualTo(hist.P95);
    }

    /// <summary>
    ///     Тест 2: Метрики экспортируются через Meter.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Метрики экспортируются через Meter")]
    public void Should_ExportMetricsViaMeter_When_Recorded()
    {
        using var meter = new Meter(MetricsMiddleware.MeterName);
        var factory = new TestMeterFactory(meter);
        var stats = new CustomStats(factory);
        using var listener = new MeterListener();
        long counterValue = 0;
        double histValue = 0;

        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter == meter)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((inst, value, tags, state) =>
        {
            if (inst.Name == "my_counter")
            {
                counterValue = value;
            }
        });
        listener.SetMeasurementEventCallback<double>((inst, value, tags, state) =>
        {
            if (inst.Name == "my_hist")
            {
                histValue = value;
            }
        });
        listener.Start();

        stats.Increment("my_counter");
        stats.Record("my_hist", 2);
        listener.RecordObservableInstruments();

        counterValue.Should().Be(1);
        histValue.Should().Be(2);
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
