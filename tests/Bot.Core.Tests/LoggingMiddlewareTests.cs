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
///     Тесты логирующего middleware.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется добавление идентификаторов, усечение текста и логирование имени обработчика</item>
///         <item>Проверяется логирование ошибок обработчика с именем</item>
///         <item>Проверяется учёт <c>web_app_data</c>.</item>
///     </list>
/// </remarks>
public sealed class LoggingMiddlewareTests
{
    /// <summary>
    ///     Тест 1: Лог содержит идентификаторы, усечённый текст и имя обработчика
    /// </summary>
    [Fact(DisplayName = "Тест 1: Лог содержит идентификаторы, усечённый текст и имя обработчика")]
    public async Task Should_LogHandlerName_WithTruncatedText_When_Invoked()
    {
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        services.AddTransient<TestHandler>();
        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<LoggingMiddleware>>();
        var mw = new LoggingMiddleware(logger);
        var registry = new HandlerRegistry();
        registry.Register(typeof(TestHandler));
        var stats = new StatsCollector();
        var router = new RouterMiddleware(sp, registry, stats);

        var longText = new string('a', 200);
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            longText,
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

        await mw.InvokeAsync(ctx, c => router.InvokeAsync(c, _ => Task.CompletedTask));

        var entry = provider.Logs.Should().ContainSingle(e => e.Message == "update").Subject;
        entry.Scope["UpdateId"].Should().Be("1");
        entry.Scope["ChatId"].Should().Be(1L);
        entry.Scope["UserId"].Should().Be(2L);
        entry.Scope["MessageId"].Should().Be(3);
        entry.Scope["UpdateType"].Should().Be("message");
        entry.Scope["Text"].Should().Be(longText[..128]);

        provider.Logs.Should().Contain(e => e.Message.StartsWith("handler TestHandler finished"));
    }

    /// <summary>
    ///     Тест 2: Ошибка обработчика логируется с именем и длительностью
    /// </summary>
    [Fact(DisplayName = "Тест 2: Ошибка обработчика логируется с именем и длительностью")]
    public async Task Should_LogError_WithHandlerName_When_HandlerThrows()
    {
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        services.AddTransient<FailingHandler>();
        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<LoggingMiddleware>>();
        var mw = new LoggingMiddleware(logger);
        var registry = new HandlerRegistry();
        registry.Register(typeof(FailingHandler));
        var stats = new StatsCollector();
        var router = new RouterMiddleware(sp, registry, stats);

        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            "hi",
            "fail",
            null,
            null,
            new Dictionary<string, object>
            {
                [UpdateItems.MessageId] = 3,
                [UpdateItems.UpdateType] = "message"
            },
            sp,
            default);

        var act = async () => { await mw.InvokeAsync(ctx, c => router.InvokeAsync(c, _ => Task.CompletedTask)); };

        await act.Should().ThrowAsync<InvalidOperationException>();

        provider.Logs.Should().Contain(e =>
            e.Level == LogLevel.Error && e.Message.StartsWith("handler FailingHandler failed"));
    }

    /// <summary>
    ///     Тест 3: Счётчик передачи данных увеличивается при обработке <c>web_app_data</c>
    /// </summary>
    [Fact(DisplayName = "Тест 3: Счётчик передачи данных увеличивается при обработке web_app_data")]
    public async Task Should_MarkSendData_When_WebAppDataHandled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<WebAppStatsCollector>();
        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<LoggingMiddleware>>();
        var mw = new LoggingMiddleware(logger);
        var stats = sp.GetRequiredService<WebAppStatsCollector>();

        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            "data",
            null,
            null,
            null,
            new Dictionary<string, object>
            {
                [UpdateItems.MessageId] = 3,
                [UpdateItems.UpdateType] = "message",
                [UpdateItems.WebAppData] = true
            },
            sp,
            default);

        await mw.InvokeAsync(ctx, _ => Task.CompletedTask);

        var snapshot = stats.GetSnapshot();
        snapshot.SendDataTotal.Should().Be(1);
    }

    [Command("test")]
    private sealed class TestHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx)
        {
            return Task.CompletedTask;
        }
    }

    [Command("fail")]
    private sealed class FailingHandler : IUpdateHandler
    {
        public Task HandleAsync(UpdateContext ctx)
        {
            throw new InvalidOperationException("fail");
        }
    }
}
