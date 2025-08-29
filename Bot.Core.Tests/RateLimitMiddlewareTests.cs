using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Options;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="RateLimitMiddleware"/>.
/// </summary>
public class RateLimitMiddlewareTests
{
    /// <summary>
    ///     При превышении лимита для пользователя запрос блокируется.
    /// </summary>
    [Fact(DisplayName = "Тест 1. При превышении лимита для пользователя запрос блокируется")]
    public async Task Exceeding_user_limit_blocks_request()
    {
        var options = new RateLimitOptions
        {
            PerUserPerMinute = 1,
            PerChatPerMinute = int.MaxValue,
            Mode = RateLimitMode.Hard
        };
        var tx = new DummyTransportClient();
        var mw = new RateLimitMiddleware(options, tx);
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

        Assert.Equal(1, calls);
        Assert.Empty(tx.SentTexts);
    }

    /// <summary>
    ///     При мягком режиме превышение лимита для чата приводит к ответу "помедленнее".
    /// </summary>
    [Fact(DisplayName = "Тест 2. При мягком режиме превышение лимита для чата приводит к ответу \"помедленнее\"")]
    public async Task Soft_mode_sends_warning_when_chat_limit_exceeded()
    {
        var options = new RateLimitOptions
        {
            PerUserPerMinute = int.MaxValue,
            PerChatPerMinute = 1,
            Mode = RateLimitMode.Soft
        };
        var tx = new DummyTransportClient();
        var mw = new RateLimitMiddleware(options, tx);
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

        Assert.Equal(1, calls);
        Assert.Single(tx.SentTexts);
        Assert.Equal("помедленнее", tx.SentTexts[0].text);
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
