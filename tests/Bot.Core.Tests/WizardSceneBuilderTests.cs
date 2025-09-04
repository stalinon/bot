using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Scenes;
using Bot.TestKit;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты построителя сцен.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяются переходы между шагами.</item>
///         <item>Проверяется истечение TTL шага.</item>
///     </list>
/// </remarks>
public sealed class WizardSceneBuilderTests
{
    /// <summary>
    ///     Тест 1: Должен пройти все шаги и вызвать завершение.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен пройти все шаги и вызвать завершение")]
    public async Task Should_WalkThroughSteps()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store, TimeSpan.FromMinutes(1));
        var client = new DummyTransportClient();
        var services = new ServiceCollection().AddSingleton<ITransportClient>(client).BuildServiceProvider();
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);
        IReadOnlyDictionary<int, string>? result = null;
        var scene = new WizardSceneBuilder()
            .AddStep("имя?", Validators.NotEmpty)
            .AddStep("возраст?", Validators.IsInt)
            .OnFinish((c, data) =>
            {
                result = data;
                return Task.CompletedTask;
            })
            .Build("profile", navigator, client);

        await navigator.EnterAsync(ctx, scene);
        var ctx2 = ctx with { Text = "иван" };
        await scene.OnUpdate(ctx2);
        var ctx3 = ctx with { Text = "30" };
        await scene.OnUpdate(ctx3);

        result.Should().NotBeNull();
        result![0].Should().Be("иван");
        result[1].Should().Be("30");
        var state = await navigator.GetStateAsync(ctx);
        state.Should().BeNull();
    }

    /// <summary>
    ///     Тест 2: Должен выйти по истечению TTL шага.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен выйти по истечению TTL шага")]
    public async Task Should_ExpireByTtl()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store, TimeSpan.FromMilliseconds(100));
        var client = new DummyTransportClient();
        var services = new ServiceCollection().AddSingleton<ITransportClient>(client).BuildServiceProvider();
        var ctx = new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(2),
            null,
            null,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);
        var scene = new WizardSceneBuilder()
            .AddStep("имя?", Validators.NotEmpty, ttl: TimeSpan.FromMilliseconds(100))
            .Build("profile", navigator, client);

        await navigator.EnterAsync(ctx, scene);
        await Task.Delay(200);
        var state = await navigator.GetStateAsync(ctx);
        state.Should().BeNull();
    }
}
