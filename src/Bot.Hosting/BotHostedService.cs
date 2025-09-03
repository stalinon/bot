using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;

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
    private UpdateDelegate? _app;
    private Channel<UpdateContext>? _channel;
    private Task? _processing;
    private Task? _writing;
    private CancellationTokenSource? _cts;
    private readonly BotOptions _options = options.Value;

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

        _app = pipeline.Build(_ => Task.CompletedTask);

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
                await _app(ctx);
                stats.SetQueueDepth(_channel.Reader.Count);
            });

        _writing = source.StartAsync(
            async ctx =>
            {
                await _channel.Writer.WriteAsync(ctx, _cts.Token);
                stats.SetQueueDepth(_channel.Reader.Count);
            },
            _cts.Token);

        _writing.ContinueWith(t => _channel.Writer.TryComplete(t.Exception), TaskScheduler.Current);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("bot hosted service stopping");
        _cts?.Cancel();
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

        if (tasks.Count > 0)
        {
            using var timeout = new CancellationTokenSource(_options.DrainTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            try
            {
                await Task.WhenAll(tasks).WaitAsync(linked.Token);
                logger.LogInformation(
                    "bot hosted service stopped: writing {WritingStatus}, processing {ProcessingStatus}",
                    _writing?.Status,
                    _processing?.Status);
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("bot hosted service drain timeout {Timeout} exceeded", _options.DrainTimeout);
            }
        }
    }
}
