using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Core.Routing;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Tests.Shared;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты маршрутизирующего middleware.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется логирование имени обработчика</item>
///     </list>
/// </remarks>
public sealed class RouterMiddlewareTests
{
    /// <summary>
    ///     Тест 1: Имя обработчика фиксируется в логах
    /// </summary>
    [Fact(DisplayName = "Тест 1: Имя обработчика фиксируется в логах")]
    public async Task Should_LogHandlerName_When_HandlerFound()
    {
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        services.AddTransient<TestHandler>();
        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<LoggingMiddleware>>();
        var logging = new LoggingMiddleware(logger);
        var registry = new HandlerRegistry();
        registry.Register(typeof(TestHandler));
        var stats = new StatsCollector();
        var router = new RouterMiddleware(sp, registry, stats);

        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            "hi",
            "test",
            null,
            null,
            new Dictionary<string, object>
            {
                [UpdateItems.MessageId] = 3,
                [UpdateItems.UpdateType] = "message"
            },
            sp,
            default);

        await logging.InvokeAsync(ctx, c => router.InvokeAsync(c, _ => ValueTask.CompletedTask))
            .ConfigureAwait(false);

        ctx.GetItem<string>(UpdateItems.Handler).Should().Be(nameof(TestHandler));
        provider.Logs.Should().Contain(e => e.Message.StartsWith("handler TestHandler finished"));
    }

    [Command("test")]
    private sealed class TestHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx)
        {
            return Task.CompletedTask;
        }
    }
}
