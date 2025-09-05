using System.Diagnostics.Metrics;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Core.Pipeline;
using Bot.Core.Routing;
using Bot.Core.Stats;
using Bot.Core.Utils;
using Bot.Hosting;
using Bot.Hosting.Options;
using Bot.TestKit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Интеграционные тесты локального пайплайна.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется обработка обновлений из файла</item>
///         <item>Проверяется подсчёт состояния</item>
///     </list>
/// </remarks>
public class PipelineIntegrationTests
{
    /// <summary>
    ///     Пайплайн обрабатывает апдейт из JSON.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Пайплайн обрабатывает апдейт из JSON", Skip = "нестабильный тест")]
    public async Task Pipeline_processes_json_update()
    {
        var updatePath = Path.Combine(AppContext.BaseDirectory, "Updates", "ping.json");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IMeterFactory, DummyMeterFactory>();
        services.AddSingleton(new TtlCache<string>(TimeSpan.FromMinutes(5)));
        services.AddSingleton(new RateLimitOptions
        { PerUserPerMinute = 100, PerChatPerMinute = 100, Mode = RateLimitMode.Soft });
        services.AddSingleton<ITransportClient, FakeTransportClient>();
        services.AddSingleton<IStateStore, InMemoryStateStore>();
        var registry = new HandlerRegistry();
        registry.Register(typeof(PingHandler));
        services.AddSingleton(registry);
        services.AddScoped<PingHandler>();
        services.AddScoped<ExceptionHandlingMiddleware>();
        services.AddScoped<MetricsMiddleware>();
        services.AddScoped<LoggingMiddleware>();
        services.AddScoped<DedupMiddleware>();
        services.AddScoped<RateLimitMiddleware>();
        services.AddScoped<CommandParsingMiddleware>();
        services.AddScoped<RouterMiddleware>();
        services.AddSingleton<StatsCollector>();
        services.AddSingleton<IUpdatePipeline, PipelineBuilder>();
        services.AddSingleton<IEnumerable<Action<IUpdatePipeline>>>(Array.Empty<Action<IUpdatePipeline>>());
        services.AddSingleton<IUpdateSource>(new JsonUpdateSource(updatePath));
        services.AddSingleton<ILogger<BotHostedService>>(sp =>
            sp.GetRequiredService<ILoggerFactory>().CreateLogger<BotHostedService>());
        services.AddOptions<BotOptions>();

        var sp = services.BuildServiceProvider();
        var pipeline = sp.GetRequiredService<IUpdatePipeline>();
        foreach (var cfg in sp.GetRequiredService<IEnumerable<Action<IUpdatePipeline>>>())
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

        var app = pipeline.Build(_ => Task.CompletedTask);
        var ctx = new UpdateContext(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            "/ping",
            null,
            null,
            null,
            new Dictionary<string, object>(),
            sp,
            default);

        await app(ctx);

        var tx = (FakeTransportClient)sp.GetRequiredService<ITransportClient>();
        Assert.Contains(tx.SentTexts, m => m.text == "pong");

        var store = (InMemoryStateStore)sp.GetRequiredService<IStateStore>();
        var value = await store.GetAsync<long>("user", "ping:1", default);
        Assert.Equal(1, value);
    }

    [Command("ping")]
    private sealed class PingHandler(IStateStore store, ITransportClient tx) : IUpdateHandler
    {
        /// <inheritdoc />
        public async Task HandleAsync(UpdateContext ctx)
        {
            var key = $"ping:{ctx.User.Id}";
            var n = await store.IncrementAsync("user", key, 1, null, ctx.CancellationToken);
            await tx.SendTextAsync(ctx.Chat, "pong", ctx.CancellationToken);
        }
    }

    private sealed class DummyMeterFactory : IMeterFactory
    {
        /// <inheritdoc />
        public Meter Create(MeterOptions options)
        {
            return new Meter(options.Name, options.Version);
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public Meter Create(string name, string? version = null)
        {
            return Create(new MeterOptions(name) { Version = version });
        }
    }
}
