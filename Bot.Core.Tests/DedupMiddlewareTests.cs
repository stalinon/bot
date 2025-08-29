using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Core.Middlewares;
using Bot.Core.Utils;
using Bot.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="DedupMiddleware"/>.
/// </summary>
public class DedupMiddlewareTests
{
    /// <summary>
    ///     Дубликаты игнорируются в течение TTL и принимаются после его истечения.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Дубликаты игнорируются в течение TTL и принимаются после его истечения")]
    public async Task Duplicate_within_ttl_is_ignored_and_after_ttl_passes()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        using var cache = new TtlCache<string>(TimeSpan.FromMilliseconds(100));
        var mw = new DedupMiddleware(loggerFactory.CreateLogger<DedupMiddleware>(), cache);
        var ctx = new UpdateContext(
            Transport: "test",
            UpdateId: "42",
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
        await mw.InvokeAsync(ctx, next); // дубликат в пределах TTL
        Assert.Equal(1, calls);

        await Task.Delay(250); // ждём окончания TTL и очистки
        await mw.InvokeAsync(ctx, next); // после TTL должен пройти
        Assert.Equal(2, calls);
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
