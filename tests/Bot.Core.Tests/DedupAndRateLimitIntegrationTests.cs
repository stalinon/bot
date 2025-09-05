using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Core.Pipeline;
using Bot.Core.Stats;
using Bot.Core.Utils;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Интеграционные тесты пачки обновлений с дедупликацией и лимитами.
/// </summary>
public class DedupAndRateLimitIntegrationTests
{
    /// <summary>
    ///     Дубликаты и обновления сверх лимита не доходят до конечного обработчика.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Дубликаты и превышения лимита не доходят до обработчика")]
    public async Task Duplicates_and_excess_updates_are_filtered()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new TtlCache<string>(TimeSpan.FromMinutes(1)));
        services.AddSingleton(new RateLimitOptions
        { PerUserPerMinute = 3, PerChatPerMinute = 3, Mode = RateLimitMode.Soft });
        services.AddScoped<DedupMiddleware>();
        services.AddSingleton<RateLimitMiddleware>();
        services.AddSingleton<ITransportClient, DummyTransportClient>();
        services.AddSingleton<StatsCollector>();

        using var sp = services.BuildServiceProvider();
        var pipeline = new PipelineBuilder(sp.GetRequiredService<IServiceScopeFactory>());
        pipeline.Use<DedupMiddleware>();
        pipeline.Use<RateLimitMiddleware>();

        var handled = 0;
        pipeline.Use(next => async ctx =>
        {
            Interlocked.Increment(ref handled);
            await next(ctx).ConfigureAwait(false);
        });

        var app = pipeline.Build(_ => ValueTask.CompletedTask);

        foreach (var id in new[] { "1", "1", "2", "3", "4" })
        {
            var ctx = new UpdateContext(
                "test",
                id,
                new ChatAddress(1),
                new UserAddress(1),
                null,
                null,
                null,
                null,
                new Dictionary<string, object>(),
                sp,
                CancellationToken.None);

            await app(ctx).ConfigureAwait(false);
        }

        Assert.Equal(3, handled);
    }
}
