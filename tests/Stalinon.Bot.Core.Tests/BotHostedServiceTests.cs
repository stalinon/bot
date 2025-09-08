using System.Diagnostics.Metrics;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Pipeline;
using Stalinon.Bot.Core.Routing;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Core.Utils;
using Stalinon.Bot.Hosting;
using Stalinon.Bot.Hosting.Options;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты параллельной обработки в <see cref="BotHostedService" />.
/// </summary>
public class BotHostedServiceTests
{
    /// <summary>
    ///     Тест 1: Параллельная обработка ограничена
    /// </summary>
    [Fact(DisplayName = "Тест 1: Параллельная обработка ограничена")]
    public async Task Should_LimitParallelism_When_ProcessingManyUpdates()
    {
        const int updates = 20;
        const int parallelism = 4;
        var tracker = new ConcurrencyTracker();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMeterFactory, DummyMeterFactory>();
        services.AddOptions<RateLimitOptions>().Configure(o =>
        {
            o.PerUserPerMinute = int.MaxValue;
            o.PerChatPerMinute = int.MaxValue;
            o.Mode = RateLimitMode.Soft;
        });
        services.AddOptions<DeduplicationOptions>().Configure(o =>
            o.Window = TimeSpan.FromMinutes(5));
        services.AddSingleton(new TtlCache<string>(TimeSpan.FromMinutes(5)));

        services.AddSingleton<ITransportClient, DummyTransportClient>();
        services.AddSingleton(new HandlerRegistry());
        services.AddScoped<ExceptionHandlingMiddleware>();
        services.AddScoped<MetricsMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<DedupMiddleware>();
        services.AddScoped<RateLimitMiddleware>();
        services.AddScoped<CommandParsingMiddleware>();
        services.AddScoped<RouterMiddleware>();
        services.AddSingleton<StatsCollector>();
        services.AddSingleton<IUpdatePipeline, PipelineBuilder>();
        services.AddSingleton<IEnumerable<Action<IUpdatePipeline>>>(new[]
            { (Action<IUpdatePipeline>)(p => p.Use(tracker.Middleware)) });
        services.AddSingleton<IUpdateSource>(new TestUpdateSource(updates));
        services.AddSingleton<ILogger<BotHostedService>>(sp =>
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<BotHostedService>());
        services.AddOptions<BotOptions>().Configure(o => o.Transport.Parallelism = parallelism);
        services.AddOptions<StopOptions>();

        var sp = services.BuildServiceProvider();
        var svc = ActivatorUtilities.CreateInstance<BotHostedService>(sp);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        await svc.StartAsync(cts.Token).ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        await svc.StopAsync(CancellationToken.None).ConfigureAwait(false);

        Assert.True(tracker.MaxActive <= parallelism, $"max {tracker.MaxActive} > {parallelism}");
    }

    private sealed class TestUpdateSource(int count) : IUpdateSource
    {
        public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
        {
            for (var i = 0; i < count; i++)
            {
                var ctx = new UpdateContext(
                    "test",
                    i.ToString(),
                    new ChatAddress(1),
                    new UserAddress(1),
                    null,
                    null,
                    null,
                    null,
                    new Dictionary<string, object>(),
                    null!,
                    ct);
                await onUpdate(ctx).ConfigureAwait(false);
            }

            try
            {
                await Task.Delay(Timeout.Infinite, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }
    }

    private sealed class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options)
        {
            return new Meter(options.Name, options.Version);
        }

        public void Dispose()
        {
        }

        public Meter Create(string name, string? version = null)
        {
            return Create(new MeterOptions(name) { Version = version });
        }
    }

    private sealed class ConcurrencyTracker
    {
        private int _active;
        private int _max;

        public int MaxActive => _max;

        public UpdateDelegate Middleware(UpdateDelegate next)
        {
            return async ctx =>
            {
                var current = Interlocked.Increment(ref _active);
                int prev;
                do
                {
                    prev = _max;
                    if (current <= prev)
                    {
                        break;
                    }
                } while (Interlocked.CompareExchange(ref _max, current, prev) != prev);

                await Task.Delay(50).ConfigureAwait(false);
                await next(ctx).ConfigureAwait(false);
                Interlocked.Decrement(ref _active);
            };
        }
    }
}
