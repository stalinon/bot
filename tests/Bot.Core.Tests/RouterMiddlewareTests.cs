using System.Collections.Generic;
using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Routing;
using Bot.Core.Stats;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Bot.Core.Tests;

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
            "/test",
            null,
            null,
            new Dictionary<string, object>
            {
                [UpdateItems.MessageId] = 3,
                [UpdateItems.UpdateType] = "message"
            },
            sp,
            default);

        await logging.InvokeAsync(ctx, c => router.InvokeAsync(c, _ => Task.CompletedTask));

        ctx.GetItem<string>(UpdateItems.Handler).Should().Be(nameof(TestHandler));
        provider.Logs.Should().Contain(e => e.Message.StartsWith("handler TestHandler finished"));
    }

    [Command("/test")]
    private sealed class TestHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx) => Task.CompletedTask;
    }
}
