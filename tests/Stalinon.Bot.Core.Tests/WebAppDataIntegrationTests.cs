using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Core.Routing;
using Stalinon.Bot.Core.Stats;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Интеграционные тесты обработки данных веб-приложения
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется маршрутизация при наличии WebAppData</item>
///     </list>
/// </remarks>
public sealed class WebAppDataIntegrationTests
{
    /// <summary>
    ///     Тест 1: Данные WebApp направляются в обработчик с фильтром
    /// </summary>
    [Fact(DisplayName = "Тест 1: Данные WebApp направляются в обработчик с фильтром")]
    public async Task Should_RouteWebAppData_When_FilterSet()
    {
        var items = new Dictionary<string, object>
        {
            [UpdateItems.UpdateType] = "message",
            [UpdateItems.MessageId] = 2,
            [UpdateItems.WebAppData] = true
        };
        var ctx = new UpdateContext(
            "telegram",
            "1",
            new ChatAddress(1),
            new UserAddress(3),
            "btn",
            null,
            null,
            "42",
            items,
            null!,
            default);
        var handled = false;
        var services = new ServiceCollection();
        services.AddSingleton(new StatsCollector());
        services.AddTransient<TestHandler>(_ => new TestHandler(() => handled = true));
        var sp = services.BuildServiceProvider();
        var registry = new HandlerRegistry();
        registry.Register(typeof(TestHandler));
        var router = new RouterMiddleware(sp, registry, sp.GetRequiredService<StatsCollector>());
        ctx = ctx with { Services = sp };

        await router.InvokeAsync(ctx, _ => ValueTask.CompletedTask).ConfigureAwait(false);

        handled.Should().BeTrue();
        ctx.Payload.Should().Be("42");
        ctx.GetItem<bool>(UpdateItems.WebAppData).Should().BeTrue();
    }

    [TextMatch(".*")]
    [UpdateFilter(WebAppData = true)]
    private sealed class TestHandler(Action onHandled) : IUpdateHandler
    {
        private readonly Action _onHandled = onHandled;

        public Task HandleAsync(UpdateContext ctx)
        {
            _onHandled();
            return Task.CompletedTask;
        }
    }
}
