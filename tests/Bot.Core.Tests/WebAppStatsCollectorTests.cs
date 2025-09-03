using System.Diagnostics.Metrics;

using Bot.Core.Middlewares;
using Bot.Core.Stats;

using FluentAssertions;

using Microsoft.Extensions.Diagnostics;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="WebAppStatsCollector"/>.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется подсчёт авторизации и профиля.</item>
///         <item>Проверяется экспорт метрик через <see cref="Meter"/>.</item>
///     </list>
/// </remarks>
public sealed class WebAppStatsCollectorTests
{
    /// <inheritdoc />
    public WebAppStatsCollectorTests()
    {
    }

    /// <summary>
    ///     Тест 1: Счётчики авторизации и профиля учитываются.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Счётчики авторизации и профиля учитываются")]
    public void Should_CountAuthAndMe_When_Marked()
    {
        var stats = new WebAppStatsCollector();
        stats.MarkAuth(10);
        stats.MarkMe(20);

        var snapshot = stats.GetSnapshot();
        snapshot.AuthTotal.Should().Be(1);
        snapshot.MeTotal.Should().Be(1);
        snapshot.P95.Should().BeGreaterOrEqualTo(snapshot.P50);
        snapshot.P99.Should().BeGreaterOrEqualTo(snapshot.P95);
    }

    /// <summary>
    ///     Тест 2: Метрики экспортируются через Meter.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Метрики экспортируются через Meter")]
    public void Should_ExportMetricsViaMeter_When_Marked()
    {
        using var meter = new Meter(MetricsMiddleware.MeterName);
        var factory = new TestMeterFactory(meter);
        var stats = new WebAppStatsCollector(factory);
        using var listener = new MeterListener();
        long auth = 0;
        long me = 0;
        double latency = 0;

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
                case "tgbot_webapp_auth_total":
                    auth = value;
                    break;
                case "tgbot_webapp_me_total":
                    me = value;
                    break;
            }
        });
        listener.SetMeasurementEventCallback<double>((inst, value, tags, state) =>
        {
            if (inst.Name == "tgbot_webapp_request_latency_ms")
            {
                latency = value;
            }
        });
        listener.Start();

        stats.MarkAuth(5);
        stats.MarkMe(7);

        listener.RecordObservableInstruments();

        auth.Should().Be(1);
        me.Should().Be(1);
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

