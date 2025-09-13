using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;

using Stalinon.Bot.Outbox;

using Xunit;

namespace Stalinon.Bot.Outbox.Tests;

/// <summary>
///     Тесты для <see cref="FileOutbox" />: проверка отправки, повторов и списания в deadletter
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Добавляет сообщение и учитывает его в ожидании.</item>
///         <item>Подтверждает отправку и удаляет файл.</item>
///         <item>Повторяет отправку при ошибке и считает повторы.</item>
///         <item>Помещает сообщение в deadletter после превышения попыток.</item>
///         <item>Пробрасывает исключения при ошибке записи и чтения файлов.</item>
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

    /// <summary>
    ///     Тест 4: Должен добавлять сообщение и учитывать его в ожидании
    /// </summary>
    [Fact(DisplayName = "Тест 4: Должен добавлять сообщение и учитывать его в ожидании")]
    public async Task Should_AddMessage_When_TransportDelayed()
    {
        using var meter = new Meter("tgbot");
        var factory = new TestMeterFactory(meter);
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outbox = new FileOutbox(temp, meterFactory: factory);
        var tcs = new TaskCompletionSource();
        var sendTask = outbox.SendAsync("1", "payload", async (_, _, _) => await tcs.Task, CancellationToken.None);

        await Task.Delay(100);
        File.Exists(Path.Combine(temp, "1.json")).Should().BeTrue();
        var pending = await outbox.GetPendingAsync(CancellationToken.None);
        pending.Should().Be(1);

        tcs.SetResult();
        await sendTask;
    }

    /// <summary>
    ///     Тест 5: Должен подтверждать сообщение и удалять файл после успешной отправки
    /// </summary>
    [Fact(DisplayName = "Тест 5: Должен подтверждать сообщение и удалять файл после успешной отправки")]
    public async Task Should_AckMessage_When_TransportCompletes()
    {
        using var meter = new Meter("tgbot");
        var factory = new TestMeterFactory(meter);
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outbox = new FileOutbox(temp, meterFactory: factory);
        var tcs = new TaskCompletionSource();
        var sendTask = outbox.SendAsync("1", "payload", async (_, _, _) => await tcs.Task, CancellationToken.None);

        await Task.Delay(100);
        File.Exists(Path.Combine(temp, "1.json")).Should().BeTrue();
        tcs.SetResult();
        await sendTask;

        File.Exists(Path.Combine(temp, "1.json")).Should().BeFalse();
        var pending = await outbox.GetPendingAsync(CancellationToken.None);
        pending.Should().Be(0);
    }

    /// <summary>
    ///     Тест 6: Должен повторно обрабатывать сообщение после ошибки транспорта
    /// </summary>
    [Fact(DisplayName = "Тест 6: Должен повторно обрабатывать сообщение после ошибки транспорта")]
    public async Task Should_ReprocessMessage_When_TransportFailsInitially()
    {
        using var meter = new Meter("tgbot");
        var factory = new TestMeterFactory(meter);
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outbox = new FileOutbox(temp, meterFactory: factory);
        var tcs = new TaskCompletionSource();
        var attempt = 0;
        var sendTask = outbox.SendAsync("1", "payload", async (_, _, _) =>
        {
            attempt++;
            if (attempt == 1)
            {
                throw new InvalidOperationException();
            }

            await tcs.Task;
        }, CancellationToken.None);

        await Task.Delay(100);
        var file = Path.Combine(temp, "1.json");
        File.Exists(file).Should().BeTrue();
        var content = await File.ReadAllTextAsync(file);
        var record = JsonSerializer.Deserialize<RecordDto>(content);
        record!.Attempt.Should().Be(1);

        tcs.SetResult();
        await sendTask;

        File.Exists(file).Should().BeFalse();
    }

    /// <summary>
    ///     Тест 7: Должен выбрасывать исключение при ошибке записи файла
    /// </summary>
    [Fact(DisplayName = "Тест 7: Должен выбрасывать исключение при ошибке записи файла")]
    public async Task Should_Throw_OnWriteError()
    {
        using var meter = new Meter("tgbot");
        var factory = new TestMeterFactory(meter);
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outbox = new FileOutbox(temp, meterFactory: factory);
        var id = new string('a', 300);
        var act = () => outbox.SendAsync(id, "payload", (_, _, _) => Task.CompletedTask, CancellationToken.None);
        await act.Should().ThrowAsync<Exception>();
    }

    /// <summary>
    ///     Тест 8: Должен выбрасывать исключение при ошибке чтения файла
    /// </summary>
    [Fact(DisplayName = "Тест 8: Должен выбрасывать исключение при ошибке чтения файла")]
    public async Task Should_Throw_OnReadError()
    {
        using var meter = new Meter("tgbot");
        var factory = new TestMeterFactory(meter);
        var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var outbox = new FileOutbox(temp, meterFactory: factory);
        Directory.Delete(temp, true);
        var act = async () => await outbox.GetPendingAsync(CancellationToken.None);
        await act.Should().ThrowAsync<DirectoryNotFoundException>();
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

    private sealed record RecordDto(string Id, string Payload, int Attempt);
}
