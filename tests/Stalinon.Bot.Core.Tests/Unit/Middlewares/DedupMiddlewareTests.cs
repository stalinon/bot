using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Middlewares;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты для <see cref="DedupMiddleware" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Фильтрация дубликатов.</item>
///         <item>Обработка уникальных обновлений.</item>
///         <item>Учёт потерянных обновлений в статистике.</item>
///         <item>Работа в нескольких инстансах при использовании хранилища.</item>
///         <item>Проброс исключений из следующего обработчика.</item>
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
        var stats = new StatsCollector();
        using var mw = new DedupMiddleware(
            loggerFactory.CreateLogger<DedupMiddleware>(),
            Microsoft.Extensions.Options.Options.Create(new DeduplicationOptions { Window = TimeSpan.FromMilliseconds(100) }),
            stats);
        var ctx = new UpdateContext(
            "test",
            "42",
            new ChatAddress(1),
            new UserAddress(1),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            new DummyServiceProvider(),
            CancellationToken.None);
        var calls = 0;
        UpdateDelegate next = _ =>
        {
            calls++;
            return ValueTask.CompletedTask;
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
        var stats = new StatsCollector();
        using var mw = new DedupMiddleware(
            loggerFactory.CreateLogger<DedupMiddleware>(),
            Microsoft.Extensions.Options.Options.Create(new DeduplicationOptions { Window = TimeSpan.FromMilliseconds(100) }),
            stats);
        var ctx1 = new UpdateContext(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            new DummyServiceProvider(),
            CancellationToken.None);
        var ctx2 = ctx1 with { UpdateId = "2" };
        var calls = 0;
        UpdateDelegate next = _ =>
        {
            calls++;
            return ValueTask.CompletedTask;
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
        var stats = new StatsCollector();
        using var mw = new DedupMiddleware(
            loggerFactory.CreateLogger<DedupMiddleware>(),
            Microsoft.Extensions.Options.Options.Create(new DeduplicationOptions { Window = TimeSpan.FromMinutes(1) }),
            stats);
        var ctx = new UpdateContext(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            new DummyServiceProvider(),
            CancellationToken.None);

        await mw.InvokeAsync(ctx, _ => ValueTask.CompletedTask);
        await mw.InvokeAsync(ctx, _ => ValueTask.CompletedTask);

        stats.GetSnapshot().DroppedUpdates.Should().Be(1);
    }

    /// <summary>
    ///     Тест 4: Дубликат игнорируется в разных инстансах при использовании хранилища.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Дубликат игнорируется в разных инстансах при использовании хранилища")]
    public async Task Duplicate_is_ignored_across_instances_with_store()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var options = Microsoft.Extensions.Options.Options.Create(new DeduplicationOptions { Window = TimeSpan.FromMinutes(1) });
        var store = new InMemoryStateStore();
        var stats1 = new StatsCollector();
        var stats2 = new StatsCollector();
        using var mw1 = new DedupMiddleware(loggerFactory.CreateLogger<DedupMiddleware>(), options, stats1, store);
        using var mw2 = new DedupMiddleware(loggerFactory.CreateLogger<DedupMiddleware>(), options, stats2, store);
        var ctx = new UpdateContext(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            new DummyServiceProvider(),
            CancellationToken.None);
        var calls = 0;
        UpdateDelegate next = _ =>
        {
            calls++;
            return ValueTask.CompletedTask;
        };

        await mw1.InvokeAsync(ctx, next);
        await mw2.InvokeAsync(ctx, next);

        calls.Should().Be(1);
        stats2.GetSnapshot().DroppedUpdates.Should().Be(1);
    }


    /// <summary>
    ///     Тест 5: Исключение следующего обработчика пробрасывается
    /// </summary>
    [Fact(DisplayName = "Тест 5: Исключение следующего обработчика пробрасывается")]
    public async Task Should_PropagateException_FromNext()
    {
        var loggerFactory = LoggerFactory.Create(b => { });
        var stats = new StatsCollector();
        using var mw = new DedupMiddleware(
            loggerFactory.CreateLogger<DedupMiddleware>(),
            Microsoft.Extensions.Options.Options.Create(new DeduplicationOptions { Window = TimeSpan.FromMinutes(1) }),
            stats);
        var ctx = new UpdateContext(
            "test",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            new DummyServiceProvider(),
            CancellationToken.None);
        var act = async () =>
        {
            await mw.InvokeAsync(ctx, _ => throw new InvalidOperationException()).ConfigureAwait(false);
        };

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class DummyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return null;
        }
    }
}
