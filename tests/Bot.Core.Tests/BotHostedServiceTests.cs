using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Core.Pipeline;
using Bot.Core.Routing;
using Bot.Core.Utils;
using Bot.Core.Stats;
using Bot.Hosting;
using Bot.Hosting.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты параллельной обработки в <see cref="BotHostedService"/>.
/// </summary>
public class BotHostedServiceTests
{
    /// <summary>
    ///     Параллельная обработка не превышает заданный предел.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Параллельная обработка ограничена")]
    public async Task Parallelism_is_limited()
    {
        const int updates = 20;
        const int parallelism = 4;
        var tracker = new ConcurrencyTracker();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMeterFactory, DummyMeterFactory>();
        services.AddSingleton(new TtlCache<string>(TimeSpan.FromMinutes(5)));
        services.AddSingleton<RateLimitOptions>(new RateLimitOptions { PerUserPerMinute = int.MaxValue, PerChatPerMinute = int.MaxValue, Mode = RateLimitMode.Soft });
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
        services.AddSingleton<IEnumerable<Action<IUpdatePipeline>>>(new[] { (Action<IUpdatePipeline>)(p => p.Use(tracker.Middleware)) });
        services.AddSingleton<IUpdateSource>(new TestUpdateSource(updates));
        services.AddSingleton<ILogger<BotHostedService>>(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger<BotHostedService>());
        services.AddOptions<BotOptions>().Configure(o => o.Transport.Parallelism = parallelism);

        var sp = services.BuildServiceProvider();
        var svc = ActivatorUtilities.CreateInstance<BotHostedService>(sp);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));
        await svc.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(2));
        await svc.StopAsync(CancellationToken.None);

        Assert.True(tracker.MaxActive <= parallelism, $"max {tracker.MaxActive} > {parallelism}");
    }

    private sealed class TestUpdateSource(int count) : IUpdateSource
    {
        public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
        {
            for (var i = 0; i < count; i++)
            {
                var ctx = new UpdateContext(
                    Transport: "test",
                    UpdateId: i.ToString(),
                    Chat: new ChatAddress(1),
                    User: new UserAddress(1),
                    Text: null,
                    Command: null,
                    Args: null,
                    Payload: null,
                    Items: new Dictionary<string, object>(),
                    Services: null!,
                    CancellationToken: ct);
                await onUpdate(ctx);
            }

            try
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            catch (OperationCanceledException) { }
        }
    }

    private sealed class DummyMeterFactory : IMeterFactory
    {
        public Meter Create(string name, string? version = null) => this.Create(new MeterOptions(name) { Version = version });
        public Meter Create(MeterOptions options) => new(options.Name, options.Version);
        public void Dispose()
        {
        }
    }

    private sealed class ConcurrencyTracker
    {
        private int _active;
        private int _max;

        public int MaxActive => _max;

        public UpdateDelegate Middleware(UpdateDelegate next) => async ctx =>
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

            await Task.Delay(50);
            await next(ctx);
            Interlocked.Decrement(ref _active);
        };
    }
}
