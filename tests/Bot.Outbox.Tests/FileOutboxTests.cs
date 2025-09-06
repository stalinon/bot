using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Bot.Outbox;

using FluentAssertions;

using Xunit;

namespace Bot.Outbox.Tests;

/// <summary>
///     Тесты для <see cref="FileOutbox" />: проверка отправки, повторов и списания в deadletter
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Отправляет сообщение и подтверждает успешную отправку.</item>
///         <item>Повторяет отправку при ошибке и считает повторы.</item>
///         <item>Помещает сообщение в deadletter после превышения попыток.</item>
///     </list>
/// </remarks>
public sealed class FileOutboxTests
{
    /// <inheritdoc />
    public FileOutboxTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен отправлять сообщение и увеличивать счётчик отправленных
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен отправлять сообщение и увеличивать счётчик отправленных")]
    public async Task Should_SendMessage_When_TransportSucceeds()
    {
        using var meter = new Meter("tgbot");
        var factory = new TestMeterFactory(meter);
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outbox = new FileOutbox(temp, meterFactory: factory);
        var sent = 0L;
        using var listener = CreateListener("tgbot_outbox_sent_total", v => sent = v);

        await outbox.SendAsync("1", "payload", (_, _, _) => Task.CompletedTask, CancellationToken.None);
        listener.RecordObservableInstruments();

        sent.Should().Be(1);
        var pending = await outbox.GetPendingAsync(CancellationToken.None);
        pending.Should().Be(0);
    }

    /// <summary>
    ///     Тест 2: Должен повторять отправку и увеличивать счётчик повторов при ошибке
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен повторять отправку и увеличивать счётчик повторов при ошибке")]
    public async Task Should_Retry_When_TransportFailsOnce()
    {
        using var meter = new Meter("tgbot");
        var factory = new TestMeterFactory(meter);
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outbox = new FileOutbox(temp, meterFactory: factory);
        var retry = 0L;
        using var listener = CreateListener("tgbot_outbox_retry_total", v => retry = v);

        var attempt = 0;
        await outbox.SendAsync("1", "payload", (_, _, _) =>
        {
            attempt++;
            if (attempt == 1)
            {
                throw new InvalidOperationException();
            }

            return Task.CompletedTask;
        }, CancellationToken.None);

        listener.RecordObservableInstruments();
        retry.Should().Be(1);
    }

    /// <summary>
    ///     Тест 3: Должен помещать сообщение в deadletter после превышения попыток
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен помещать сообщение в deadletter после превышения попыток")]
    public async Task Should_MoveToDeadletter_When_TransportAlwaysFails()
    {
        using var meter = new Meter("tgbot");
        var factory = new TestMeterFactory(meter);
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outbox = new FileOutbox(temp, maxAttempts: 2, meterFactory: factory);
        var dead = 0L;
        using var listener = CreateListener("tgbot_outbox_deadletter_total", v => dead = v);

        await outbox.SendAsync("1", "payload", (_, _, _) => throw new InvalidOperationException(), CancellationToken.None);
        listener.RecordObservableInstruments();

        dead.Should().Be(1);
    }

    private static MeterListener CreateListener(string name, Action<long> callback)
    {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == name)
            {
                l.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((inst, value, _, __) =>
        {
            if (inst.Name == name)
            {
                callback(value);
            }
        });
        listener.Start();
        return listener;
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
