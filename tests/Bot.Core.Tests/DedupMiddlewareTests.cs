using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Middlewares;
using Bot.Core.Stats;
using Bot.Core.Utils;
using Bot.TestKit;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="DedupMiddleware"/>.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Фильтрация дубликатов.</item>
///         <item>Обработка уникальных обновлений.</item>
///         <item>Учёт потерянных обновлений в статистике.</item>
///         <item>Работа в нескольких инстансах при использовании хранилища.</item>
///     </list>
/// </remarks>
public class DedupMiddlewareTests
{
    /// <summary>
    ///     Тест 1: Дубликаты игнорируются в течение TTL и принимаются после его истечения.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Дубликаты игнорируются в течение TTL и принимаются после его истечения")]
    public async Task Duplicate_within_ttl_is_ignored_and_after_ttl_passes()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        using var cache = new TtlCache<string>(TimeSpan.FromMilliseconds(100));
        var stats = new StatsCollector();
        var mw = new DedupMiddleware(loggerFactory.CreateLogger<DedupMiddleware>(), cache, stats);
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
        calls.Should().Be(1);

        await Task.Delay(250); // ждём окончания TTL и очистки
        await mw.InvokeAsync(ctx, next); // после TTL должен пройти
        calls.Should().Be(2);
    }

    /// <summary>
    ///     Тест 2: Уникальные обновления обрабатываются независимо.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Уникальные обновления обрабатываются независимо")]
    public async Task Different_update_ids_are_processed_independently()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        using var cache = new TtlCache<string>(TimeSpan.FromMilliseconds(100));
        var stats = new StatsCollector();
        var mw = new DedupMiddleware(loggerFactory.CreateLogger<DedupMiddleware>(), cache, stats);
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
        var ctx2 = ctx1 with { UpdateId = "2" };
        var calls = 0;
        UpdateDelegate next = _ =>
        {
            calls++;
            return Task.CompletedTask;
        };

        await mw.InvokeAsync(ctx1, next);
        await mw.InvokeAsync(ctx2, next);
        calls.Should().Be(2);
    }

    /// <summary>
    ///     Тест 3: Игнорирование дубликата увеличивает счётчик потерянных обновлений.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Игнорирование дубликата увеличивает счётчик потерянных обновлений")]
    public async Task Duplicate_increments_dropped_counter()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        using var cache = new TtlCache<string>(TimeSpan.FromMinutes(1));
        var stats = new StatsCollector();
        var mw = new DedupMiddleware(loggerFactory.CreateLogger<DedupMiddleware>(), cache, stats);
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

        stats.GetSnapshot().DroppedUpdates.Should().Be(1);
    }

    /// <summary>
    ///     Тест 4: Дубликат игнорируется в разных инстансах при использовании хранилища.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Дубликат игнорируется в разных инстансах при использовании хранилища")]
    public async Task Duplicate_is_ignored_across_instances_with_store()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        using var cache1 = new TtlCache<string>(TimeSpan.FromMinutes(1));
        using var cache2 = new TtlCache<string>(TimeSpan.FromMinutes(1));
        var store = new InMemoryStateStore();
        var stats1 = new StatsCollector();
        var stats2 = new StatsCollector();
        var mw1 = new DedupMiddleware(loggerFactory.CreateLogger<DedupMiddleware>(), cache1, stats1, store);
        var mw2 = new DedupMiddleware(loggerFactory.CreateLogger<DedupMiddleware>(), cache2, stats2, store);
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

        await mw1.InvokeAsync(ctx, next);
        await mw2.InvokeAsync(ctx, next);

        calls.Should().Be(1);
        stats2.GetSnapshot().DroppedUpdates.Should().Be(1);
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
