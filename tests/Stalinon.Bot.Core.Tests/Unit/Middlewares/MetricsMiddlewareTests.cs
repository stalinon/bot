using System.Diagnostics.Metrics;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Metrics;
using Stalinon.Bot.Core.Middlewares;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="MetricsMiddleware" />
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Запись метрик при успешной обработке</item>
///         <item>Учёт ошибок и повторный выброс исключения</item>
///         <item>Игнорирование ошибок при отмене</item>
///     </list>
/// </remarks>
public sealed class MetricsMiddlewareTests
{
    /// <inheritdoc />
    public MetricsMiddlewareTests()
    {
    }

    /// <summary>
    ///     Тест 1: Успешная обработка учитывается в счётчиках
    /// </summary>
    [Fact(DisplayName = "Тест 1: Успешная обработка учитывается в счётчиках")]
    public async Task Should_RecordMetrics_When_NextSucceeds()
    {
        // Arrange
        using var meter = new Meter(MetricsMiddleware.MeterName);
        var factory = new TestMeterFactory(meter);
        var mw = new MetricsMiddleware(factory);
        using var listener = new MeterListener();
        long updates = 0;
        long errors = 0;
        double latency = 0;
        listener.InstrumentPublished = (inst, l) =>
        {
            if (inst.Meter == meter)
            {
                l.EnableMeasurementEvents(inst);
            }
        };
        listener.SetMeasurementEventCallback<long>((inst, value, tags, state) =>
        {
            if (inst.Name == "tgbot_updates_total")
            {
                updates += value;
            }
            else if (inst.Name == "tgbot_errors_total")
            {
                errors += value;
            }
        });
        listener.SetMeasurementEventCallback<double>((inst, value, tags, state) =>
        {
            if (inst.Name == "tgbot_update_latency_ms")
            {
                latency = value;
            }
        });
        listener.Start();
        var services = new ServiceCollection().BuildServiceProvider();
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);

        // Act
        await mw.InvokeAsync(ctx, _ => ValueTask.CompletedTask).ConfigureAwait(false);
        listener.RecordObservableInstruments();

        // Assert
        updates.Should().Be(1);
        errors.Should().Be(0);
        latency.Should().BeGreaterThan(0);
    }

    /// <summary>
    ///     Тест 2: Ошибка учитывается и пробрасывается
    /// </summary>
    [Fact(DisplayName = "Тест 2: Ошибка учитывается и пробрасывается")]
    public async Task Should_RecordError_AndRethrow_When_NextThrows()
    {
        // Arrange
        using var meter = new Meter(MetricsMiddleware.MeterName);
        var factory = new TestMeterFactory(meter);
        var mw = new MetricsMiddleware(factory);
        using var listener = new MeterListener();
        long errors = 0;
        listener.InstrumentPublished = (inst, l) =>
        {
            if (inst.Meter == meter)
            {
                l.EnableMeasurementEvents(inst);
            }
        };
        listener.SetMeasurementEventCallback<long>((inst, value, tags, state) =>
        {
            if (inst.Name == "tgbot_errors_total")
            {
                errors += value;
            }
        });
        listener.Start();
        var services = new ServiceCollection().BuildServiceProvider();
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);

        // Act
        var act = async () =>
        {
            await mw.InvokeAsync(ctx, _ => throw new InvalidOperationException()).ConfigureAwait(false);
        };

        await act.Should().ThrowAsync<InvalidOperationException>();
        listener.RecordObservableInstruments();

        // Assert
        errors.Should().Be(1);
    }

    /// <summary>
    ///     Тест 3: Отмена не увеличивает счётчик ошибок
    /// </summary>
    [Fact(DisplayName = "Тест 3: Отмена не увеличивает счётчик ошибок")]
    public async Task Should_NotRecordError_When_Cancelled()
    {
        // Arrange
        using var meter = new Meter(MetricsMiddleware.MeterName);
        var factory = new TestMeterFactory(meter);
        var mw = new MetricsMiddleware(factory);
        using var listener = new MeterListener();
        long errors = 0;
        listener.InstrumentPublished = (inst, l) =>
        {
            if (inst.Meter == meter)
            {
                l.EnableMeasurementEvents(inst);
            }
        };
        listener.SetMeasurementEventCallback<long>((inst, value, tags, state) =>
        {
            if (inst.Name == "tgbot_errors_total")
            {
                errors += value;
            }
        });
        listener.Start();
        var services = new ServiceCollection().BuildServiceProvider();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            cts.Token);

        // Act
        var act = async () =>
        {
            await mw.InvokeAsync(ctx, _ => throw new OperationCanceledException(cts.Token)).ConfigureAwait(false);
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        listener.RecordObservableInstruments();

        // Assert
        errors.Should().Be(0);
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
