using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Bot.Core.Stats;

using FluentAssertions;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="RateLimitMiddleware"/>.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Блокировка при превышении лимита.</item>
///         <item>Мягкий режим с предупреждением.</item>
///         <item>Учёт ограниченных обновлений в статистике.</item>
///     </list>
/// </remarks>
public class RateLimitMiddlewareTests
{
    /// <summary>
    ///     Тест 1: При превышении лимита для пользователя запрос блокируется.
    /// </summary>
    [Fact(DisplayName = "Тест 1: При превышении лимита для пользователя запрос блокируется")]
    public async Task Exceeding_user_limit_blocks_request()
    {
        var options = new RateLimitOptions
        {
            PerUserPerMinute = 1,
            PerChatPerMinute = int.MaxValue,
            Mode = RateLimitMode.Hard
        };
        var tx = new DummyTransportClient();
        var stats = new StatsCollector();
        var mw = new RateLimitMiddleware(options, tx, stats);
        var ctx = new UpdateContext(
            Transport: "test",
            UpdateId: "1",
            Chat: new ChatAddress(1),
            User: new UserAddress(1),
            Text: null,
            Command: null,
            Args: null,
            Payload: null,
            Items: new Dictionary<string, object>(),
            Services: new DummyServiceProvider(),
            CancellationToken: CancellationToken.None);
        var calls = 0;
        UpdateDelegate next = _ =>
        {
            calls++;
            return Task.CompletedTask;
        };

        await mw.InvokeAsync(ctx, next);
        await mw.InvokeAsync(ctx, next); // превышение лимита пользователя

        calls.Should().Be(1);
        tx.SentTexts.Should().BeEmpty();
        stats.GetSnapshot().RateLimited.Should().Be(1);
    }

    /// <summary>
    ///     Тест 2: При мягком режиме превышение лимита для чата приводит к ответу "помедленнее".
    /// </summary>
    [Fact(DisplayName = "Тест 2: При мягком режиме превышение лимита для чата приводит к ответу \"помедленнее\"")]
    public async Task Soft_mode_sends_warning_when_chat_limit_exceeded()
    {
        var options = new RateLimitOptions
        {
            PerUserPerMinute = int.MaxValue,
            PerChatPerMinute = 1,
            Mode = RateLimitMode.Soft
        };
        var tx = new DummyTransportClient();
        var stats = new StatsCollector();
        var mw = new RateLimitMiddleware(options, tx, stats);
        var ctx1 = new UpdateContext(
            Transport: "test",
            UpdateId: "1",
            Chat: new ChatAddress(1),
            User: new UserAddress(1),
            Text: null,
            Command: null,
            Args: null,
            Payload: null,
            Items: new Dictionary<string, object>(),
            Services: new DummyServiceProvider(),
            CancellationToken: CancellationToken.None);
        var ctx2 = ctx1 with { UpdateId = "2", User = new UserAddress(2) };
        var calls = 0;
        UpdateDelegate next = _ =>
        {
            calls++;
            return Task.CompletedTask;
        };

        await mw.InvokeAsync(ctx1, next);
        await mw.InvokeAsync(ctx2, next); // превышение лимита чата

        calls.Should().Be(1);
        tx.SentTexts.Should().ContainSingle();
        tx.SentTexts[0].text.Should().Be("помедленнее");
        stats.GetSnapshot().RateLimited.Should().Be(1);
    }

    /// <summary>
    ///     Тест 3: Превышение лимита увеличивает счётчик ограниченных обновлений.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Превышение лимита увеличивает счётчик ограниченных обновлений")]
    public async Task Limit_exceeding_increments_counter()
    {
        var options = new RateLimitOptions
        {
            PerUserPerMinute = 1,
            PerChatPerMinute = int.MaxValue,
            Mode = RateLimitMode.Hard
        };
        var stats = new StatsCollector();
        var mw = new RateLimitMiddleware(options, new DummyTransportClient(), stats);
        var ctx = new UpdateContext(
            Transport: "test",
            UpdateId: "1",
            Chat: new ChatAddress(1),
            User: new UserAddress(1),
            Text: null,
            Command: null,
            Args: null,
            Payload: null,
            Items: new Dictionary<string, object>(),
            Services: new DummyServiceProvider(),
            CancellationToken: CancellationToken.None);

        await mw.InvokeAsync(ctx, _ => Task.CompletedTask);
        await mw.InvokeAsync(ctx, _ => Task.CompletedTask);

        stats.GetSnapshot().RateLimited.Should().Be(1);
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
