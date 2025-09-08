using System;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Stalinon.Bot.Outbox;

/// <summary>
///     Файловый аутбокс.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Хранит записи в файловой системе.</item>
///         <item>Повторяет отправку с экспоненциальной задержкой.</item>
///     </list>
/// </remarks>
public sealed class FileOutbox : IOutbox
{
    private readonly Counter<long>? _dead;
    private readonly string _path;
    private readonly Counter<long>? _retry;
    private readonly Counter<long>? _sent;
    private readonly int _maxAttempts;
    private readonly ObservableGauge<long>? _pending;

    /// <summary>
    ///     Создать файловый аутбокс.
    /// </summary>
    /// <param name="path">Путь к каталогу.</param>
    /// <param name="maxAttempts">Максимальное число попыток.</param>
    /// <param name="meterFactory">Фабрика метрик.</param>
    public FileOutbox(string path, int maxAttempts = 5, IMeterFactory? meterFactory = null)
    {
        _path = path;
        _maxAttempts = maxAttempts;
        Directory.CreateDirectory(_path);
        if (meterFactory is not null)
        {
            var meter = meterFactory.Create("tgbot");
            _sent = meter.CreateCounter<long>("tgbot_outbox_sent_total");
            _retry = meter.CreateCounter<long>("tgbot_outbox_retry_total");
            _dead = meter.CreateCounter<long>("tgbot_outbox_deadletter_total");
            _pending = meter.CreateObservableGauge<long>("tgbot_outbox_pending", CountPending);
        }
    }

    /// <summary>
    ///     Отправить сообщение.
    /// </summary>
    /// <inheritdoc />
    public async Task SendAsync(string id, string payload, Func<string, string, CancellationToken, Task> transport,
        CancellationToken ct)
    {
        var file = Path.Combine(_path, $"{id}.json");
        var record = new Record(id, payload, 0);
        await File.WriteAllTextAsync(file, JsonSerializer.Serialize(record), ct);
        while (true)
        {
            try
            {
                await transport(id, payload, ct);
                _sent?.Add(1);
                File.Delete(file);
                return;
            }
            catch
            {
                record = record with { Attempt = record.Attempt + 1 };
                if (record.Attempt >= _maxAttempts)
                {
                    _dead?.Add(1);
                    File.Delete(file);
                    return;
                }

                _retry?.Add(1);
                await File.WriteAllTextAsync(file, JsonSerializer.Serialize(record), ct);
                var delay = TimeSpan.FromSeconds(Math.Pow(2, record.Attempt));
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>
    ///     Получить количество ожидающих сообщений.
    /// </summary>
    /// <inheritdoc />
    public Task<long> GetPendingAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(CountPending());
    }

    private long CountPending()
    {
        return Directory.GetFiles(_path, "*.json").LongLength;
    }

    private sealed record Record(string Id, string Payload, int Attempt);
}
