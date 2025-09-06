using System.Threading.Channels;
using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Stats;
using Bot.Hosting.Options;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bot.Hosting;

/// <summary>
///     Фоновый сервис бота, обеспечивающий приём и обработку обновлений.
/// </summary>
public sealed class BotHostedService(
    IUpdateSource source,
    IUpdatePipeline pipeline,
    IEnumerable<Action<IUpdatePipeline>> configurePipeline,
    StatsCollector stats,
    ILogger<BotHostedService> logger,
    IOptions<BotOptions> options)
    : IHostedService
{
    private readonly BotOptions _options = options.Value;
    private readonly IUpdateSource _source = source;
    private UpdateDelegate? _app;
    private Channel<UpdateContext>? _channel;
    private CancellationTokenSource? _cts;
    private Task? _processing;
    private Task? _writing;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var cfg in configurePipeline)
        {
            cfg(pipeline);
        }

        pipeline
            .Use<ExceptionHandlingMiddleware>()
            .Use<MetricsMiddleware>()
            .Use<LoggingMiddleware>()
            .Use<DedupMiddleware>()
            .Use<RateLimitMiddleware>()
            .Use<CommandParsingMiddleware>()
            .Use<RouterMiddleware>();

        _app = pipeline.Build(_ => ValueTask.CompletedTask);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _channel = Channel.CreateBounded<UpdateContext>(
            new BoundedChannelOptions(_options.Transport.Parallelism * 16)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
        stats.SetQueueDepth(_channel.Reader.Count);

        _processing = Parallel.ForEachAsync(
            _channel.Reader.ReadAllAsync(_cts.Token),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.Transport.Parallelism,
                CancellationToken = _cts.Token
            },
            async (ctx, ct) =>
            {
                await _app(ctx).ConfigureAwait(false);
                stats.SetQueueDepth(_channel.Reader.Count);
            });

        _writing = _source.StartAsync(
            async ctx =>
            {
                await _channel.Writer.WriteAsync(ctx, _cts.Token).ConfigureAwait(false);
                stats.SetQueueDepth(_channel.Reader.Count);
            },
            _cts.Token);

        _writing.ContinueWith(t => _channel.Writer.TryComplete(t.Exception), TaskScheduler.Current);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("bot hosted service drain phase");
        await _source.StopAsync().ConfigureAwait(false);
        _channel?.Writer.TryComplete();

        var tasks = new List<Task>();
        if (_writing is not null)
        {
            tasks.Add(_writing);
        }

        if (_processing is not null)
        {
            tasks.Add(_processing);
        }

        var timeout = _options.DrainTimeout;
        var env = Environment.GetEnvironmentVariable("STOP__DRAIN_TIMEOUT_SECONDS");
        if (int.TryParse(env, out var secs))
        {
            timeout = TimeSpan.FromSeconds(secs);
        }

        if (tasks.Count > 0)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            try
            {
                await Task.WhenAll(tasks).WaitAsync(linked.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("bot hosted service drain timeout {Timeout} exceeded", timeout);
            }
        }

        var remaining = _channel?.Reader.Count ?? 0;
        if (remaining > 0)
        {
            stats.MarkLostUpdates(remaining);
            logger.LogWarning("bot hosted service lost {Remaining} updates", remaining);
        }

        logger.LogInformation("bot hosted service stop phase");
        _cts?.Cancel();

        if (tasks.Count > 0)
        {
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
                logger.LogInformation(
                    "bot hosted service stopped: writing {WritingStatus}, processing {ProcessingStatus}",
                    _writing?.Status,
                    _processing?.Status);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "bot hosted service stop error");
            }
        }
    }
}
