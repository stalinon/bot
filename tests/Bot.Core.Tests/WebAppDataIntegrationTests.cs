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
using Xunit;

namespace Bot.Core.Tests;

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
            Transport: "telegram",
            UpdateId: "1",
            Chat: new ChatAddress(1),
            User: new UserAddress(3),
            Text: "btn",
            Command: null,
            Args: null,
            Payload: "42",
            Items: items,
            Services: null!,
            CancellationToken: default);
        var handled = false;
        var services = new ServiceCollection();
        services.AddSingleton(new StatsCollector());
        services.AddTransient<TestHandler>(_ => new TestHandler(() => handled = true));
        var sp = services.BuildServiceProvider();
        var registry = new HandlerRegistry();
        registry.Register(typeof(TestHandler));
        var router = new RouterMiddleware(sp, registry, sp.GetRequiredService<StatsCollector>());
        ctx = ctx with { Services = sp };

        await router.InvokeAsync(ctx, _ => Task.CompletedTask);

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
