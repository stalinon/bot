using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Hosting.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace Bot.Hosting;

/// <summary>
///     Фоновый сервис бота, обеспечивающий приём и обработку обновлений.
/// </summary>
public sealed class BotHostedService(
    IUpdateSource source,
    IUpdatePipeline pipeline,
    IEnumerable<Action<IUpdatePipeline>> configurePipeline,
    ILogger<BotHostedService> logger,
    IOptions<BotOptions> options)
    : IHostedService
{
    private UpdateDelegate? _app;
    private Channel<UpdateContext>? _channel;
    private Task? _processing;

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

        _channel = Channel.CreateBounded<UpdateContext>(
            new BoundedChannelOptions(options.Value.Parallelism * 16)
            {
                FullMode = BoundedChannelFullMode.Wait
            });

        _processing = Parallel.ForEachAsync(
            _channel.Reader.ReadAllAsync(cancellationToken),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = options.Value.Parallelism,
                CancellationToken = cancellationToken
            },
            async (ctx, ct) => await _app(ctx));

        var writing = source.StartAsync(
            ctx => _channel.Writer.WriteAsync(ctx, cancellationToken).AsTask(),
            cancellationToken);

        writing.ContinueWith(t => _channel.Writer.TryComplete(t.Exception), TaskScheduler.Current);

        return Task.WhenAll(writing, _processing);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("bot hosted service stopping");
        _channel?.Writer.TryComplete();
        return Task.CompletedTask;
    }
}