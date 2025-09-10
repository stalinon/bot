using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Core.Scenes;
using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты переходов между сценами и отсутствия активной сцены.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется замена состояния при входе в новую сцену.</item>
///         <item>Проверяется исключение при попытке перейти без активной сцены.</item>
///     </list>
/// </remarks>
public sealed class SceneNavigatorFlowTests
{
    /// <inheritdoc/>
    public SceneNavigatorFlowTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен заменять состояние при переходе к новой сцене.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен заменять состояние при переходе к новой сцене")]
    public async Task Should_ReplaceState_WhenEnteringNewScene()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store);
        var ctx = CreateContext();
        var first = new DummyScene("first");
        var second = new DummyScene("second");

        await navigator.EnterAsync(ctx, first);
        var state1 = await navigator.GetStateAsync(ctx);
        state1!.Scene.Should().Be("first");

        await navigator.EnterAsync(ctx, second);
        var state2 = await navigator.GetStateAsync(ctx);
        state2!.Scene.Should().Be("second");
    }

    /// <summary>
    ///     Тест 2: Должен выбрасывать исключение при переходе без активной сцены.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен выбрасывать исключение при переходе без активной сцены")]
    public async Task Should_Throw_WhenNoActiveScene()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store);
        var ctx = CreateContext();

        var act = async () => await navigator.NextStepAsync(ctx);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static UpdateContext CreateContext()
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
