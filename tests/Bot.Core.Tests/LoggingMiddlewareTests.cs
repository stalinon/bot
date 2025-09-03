using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Core.Middlewares;
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
///         <item>Проверяется добавление идентификаторов и усечение текста</item>
///         <item>Проверяется логирование ошибок обработчика</item>
///     </list>
/// </remarks>
public sealed class LoggingMiddlewareTests
{

    /// <summary>
    ///     Тест 1: Лог содержит идентификаторы и усечённый текст
    /// </summary>
    [Fact(DisplayName = "Тест 1: Лог содержит идентификаторы и усечённый текст")]
    public async Task Should_AddScope_WithTruncatedText_When_Invoked()
    {
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<LoggingMiddleware>>();
        var mw = new LoggingMiddleware(logger);

        var longText = new string('a', 200);
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            longText,
            null,
            null,
            null,
            new Dictionary<string, object>
            {
                [UpdateItems.MessageId] = 3,
                [UpdateItems.UpdateType] = "message",
                [UpdateItems.Handler] = "TestHandler"
            },
            sp,
            default);

        await mw.InvokeAsync(ctx, _ => Task.CompletedTask);

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
    ///     Тест 2: Ошибка обработчика логируется с длительностью
    /// </summary>
    [Fact(DisplayName = "Тест 2: Ошибка обработчика логируется с длительностью")]
    public async Task Should_LogError_WithHandlerAndDuration_When_HandlerThrows()
    {
        var provider = new CollectingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(provider));
        var sp = services.BuildServiceProvider();
        var logger = sp.GetRequiredService<ILogger<LoggingMiddleware>>();
        var mw = new LoggingMiddleware(logger);

        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            "hi",
            null,
            null,
            null,
            new Dictionary<string, object>
            {
                [UpdateItems.MessageId] = 3,
                [UpdateItems.UpdateType] = "message",
                [UpdateItems.Handler] = "TestHandler"
            },
            sp,
            default);

        var act = async () =>
        {
            await mw.InvokeAsync(ctx, _ => throw new InvalidOperationException("fail"));
        };

        await act.Should().ThrowAsync<InvalidOperationException>();

        provider.Logs.Should().Contain(e => e.Level == LogLevel.Error && e.Message.StartsWith("handler TestHandler failed"));
    }
}
