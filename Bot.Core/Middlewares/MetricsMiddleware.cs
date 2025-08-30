using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Microsoft.Extensions.Diagnostics;
using Bot.Core.Metrics;

namespace Bot.Core.Middlewares;

/// <summary>
///     Метрики обработки обновлений
/// </summary>
public sealed class MetricsMiddleware : IUpdateMiddleware
{
    /// <summary>
    ///     Имя <see cref="Meter"/> для метрик.
    /// </summary>
    public const string MeterName = "Bot.Core.Metrics";

    private readonly Counter<long> _rps;
    private readonly Counter<long> _errors;
    private readonly Histogram<double> _latency;
    private readonly BotMetricsEventSource _eventSource;

    /// <summary>
    ///     Инициализировать middleware.
    /// </summary>
    /// <param name="meterFactory">Фабрика метрик.</param>
    public MetricsMiddleware(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _rps = meter.CreateCounter<long>("updates", unit: "count");
        _errors = meter.CreateCounter<long>("errors", unit: "count");
        _latency = meter.CreateHistogram<double>("latency", unit: "ms");
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
            _rps.Add(1);
            _latency.Record(sw.Elapsed.TotalMilliseconds);
            _eventSource.Update(sw.Elapsed.TotalMilliseconds, success);
        }
    }
}

