using System.Diagnostics;
using System.Diagnostics.Metrics;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Metrics;

namespace Bot.Core.Middlewares;

/// <summary>
///     Метрики обработки обновлений
/// </summary>
public sealed class MetricsMiddleware : IUpdateMiddleware
{
    /// <summary>
    ///     Имя <see cref="Meter" /> для метрик.
    /// </summary>
    public const string MeterName = "Bot.Core.Metrics";

    private readonly Counter<long> _errors;
    private readonly BotMetricsEventSource _eventSource;
    private readonly Histogram<double> _updateLatency;

    private readonly Counter<long> _updates;

    /// <summary>
    ///     Инициализировать middleware.
    /// </summary>
    /// <param name="meterFactory">Фабрика метрик.</param>
    public MetricsMiddleware(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _updates = meter.CreateCounter<long>("tgbot_updates_total", "count");
        _errors = meter.CreateCounter<long>("tgbot_errors_total", "count");
        _updateLatency = meter.CreateHistogram<double>("tgbot_update_latency_ms", "ms");
        _eventSource = BotMetricsEventSource.Log;
    }

    /// <inheritdoc />
    public async Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var sw = Stopwatch.StartNew();
        var success = false;
        try
        {
            await next(ctx);
            success = true;
        }
        catch (OperationCanceledException) when (ctx.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            _errors.Add(1);
            throw;
        }
        finally
        {
            sw.Stop();
            _updates.Add(1);
            _updateLatency.Record(sw.Elapsed.TotalMilliseconds);
            _eventSource.Update(sw.Elapsed.TotalMilliseconds, success);
        }
    }
}
