using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Core.Scenes;
using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты навигатора по сценам.
/// </summary>
public sealed class SceneNavigatorTests
{
    /// <summary>
    ///     Тест 1. Проверяем вход и выход из сцены.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Проверяем вход и выход из сцены")]
    public async Task EnterExit()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store);
        var ctx = Context();
        var scene = new DummyScene();

        await navigator.EnterAsync(ctx, scene);
        var state = await navigator.GetStateAsync(ctx);
        Assert.NotNull(state);
        Assert.Equal(0, state!.Step);

        await navigator.NextStepAsync(ctx);
        state = await navigator.GetStateAsync(ctx);
        Assert.Equal(1, state!.Step);

        await navigator.ExitAsync(ctx);
        state = await navigator.GetStateAsync(ctx);
        Assert.Null(state);
    }

    /// <summary>
    ///     Тест 2. Проверяем атомарность обновления шага.
    /// </summary>
    [Fact(DisplayName = "Тест 2. Проверяем атомарность обновления шага")]
    public async Task ParallelSteps()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store);
        var ctx = Context();
        var scene = new DummyScene();
        await navigator.EnterAsync(ctx, scene);

        var t1 = navigator.NextStepAsync(ctx);
        var t2 = navigator.NextStepAsync(ctx);
        await Task.WhenAll(t1, t2);

        var state = await navigator.GetStateAsync(ctx);
        Assert.Equal(2, state!.Step);
    }

    /// <summary>
    ///     Тест 3. Проверяем таймаут шага и автоматический выход.
    /// </summary>
    [Fact(DisplayName = "Тест 3. Проверяем таймаут шага и автоматический выход")]
    public async Task StepTimeout()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store, TimeSpan.FromMilliseconds(100));
        var ctx = Context();
        var scene = new DummyScene();

        await navigator.EnterAsync(ctx, scene);
        await Task.Delay(200);
        var state = await navigator.GetStateAsync(ctx);
        Assert.Null(state);
    }

    private static UpdateContext Context()
    {
        return new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            new ServiceCollection().BuildServiceProvider(),
            CancellationToken.None);
    }
}
