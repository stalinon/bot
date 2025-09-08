using System;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis;

namespace Stalinon.Bot.Outbox;

/// <summary>
///     Аутбокс на Redis.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Хранит записи в Redis.</item>
///         <item>Повторяет отправку с экспоненциальной задержкой.</item>
///     </list>
/// </remarks>
public sealed class RedisOutbox : IOutbox
{
    private readonly IDatabase _db;
    private readonly Counter<long>? _dead;
    private readonly Counter<long>? _retry;
    private readonly Counter<long>? _sent;
    private readonly string _prefix;
    private readonly int _maxAttempts;
    private readonly ObservableGauge<long>? _pending;

    /// <summary>
    ///     Создать Redis-аутбокс.
    /// </summary>
    /// <param name="db">База Redis.</param>
    ///     <param name="prefix">Префикс ключей.</param>
    ///     <param name="maxAttempts">Максимальное число попыток.</param>
    ///     <param name="meterFactory">Фабрика метрик.</param>
    public RedisOutbox(IDatabase db, string prefix = "outbox", int maxAttempts = 5,
        IMeterFactory? meterFactory = null)
    {
        _db = db;
        _prefix = prefix;
        _maxAttempts = maxAttempts;
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
        var key = Key(id);
        var record = new Record(id, payload, 0);
        await _db.StringSetAsync(key, JsonSerializer.Serialize(record));
        while (true)
        {
            try
            {
                await transport(id, payload, ct);
                _sent?.Add(1);
                await _db.KeyDeleteAsync(key);
                return;
            }
            catch
            {
                record = record with { Attempt = record.Attempt + 1 };
                if (record.Attempt >= _maxAttempts)
                {
                    _dead?.Add(1);
                    await _db.KeyDeleteAsync(key);
                    return;
                }

                _retry?.Add(1);
                await _db.StringSetAsync(key, JsonSerializer.Serialize(record));
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
        var server = _db.Multiplexer.GetServers().First();
        return server.Keys(_db.Database, $"{_prefix}:*").LongCount();
    }

    private string Key(string id)
    {
        return $"{_prefix}:{id}";
    }

    private sealed record Record(string Id, string Payload, int Attempt);
}
