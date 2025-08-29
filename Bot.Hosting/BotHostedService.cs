using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bot.Hosting;

public sealed class BotHostedService(
    IUpdateSource source,
    IUpdatePipeline pipeline,
    IEnumerable<Action<IUpdatePipeline>> configurePipeline,
    ILogger<BotHostedService> logger)
    : IHostedService
{
    private UpdateDelegate? _app;

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
        return source.StartAsync(_app.Invoke, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("bot hosted service stopping");
        return Task.CompletedTask;
    }
}