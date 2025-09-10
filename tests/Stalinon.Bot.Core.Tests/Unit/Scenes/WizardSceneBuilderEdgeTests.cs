using System;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Scenes;
using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.Core.Tests;

/// <summary>
///     Тесты мастера сцен: корректное завершение, ошибки валидаторов и отсутствие шагов.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется прохождение мастера с сохранением данных.</item>
///         <item>Проверяется обработка ошибок валидаторов.</item>
///         <item>Проверяется отсутствие шагов при запуске мастера.</item>
///     </list>
/// </remarks>
public sealed class WizardSceneBuilderEdgeTests
{
    /// <inheritdoc/>
    public WizardSceneBuilderEdgeTests()
    {
    }

    /// <summary>
    ///     Тест 1: Должен проходить шаги мастера и возвращать данные.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен проходить шаги мастера и возвращать данные")]
    public async Task Should_CollectData_OnValidInput()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store, TimeSpan.FromMinutes(1));
        var client = new DummyTransportClient();
        var services = new ServiceCollection().AddSingleton<ITransportClient>(client).BuildServiceProvider();
        var ctx = CreateContext(services);
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
        await scene.OnUpdate(ctx with { Text = "иван" });
        await scene.OnUpdate(ctx with { Text = "30" });

        result.Should().NotBeNull();
        result![0].Should().Be("иван");
        result[1].Should().Be("30");
        var state = await navigator.GetStateAsync(ctx);
        state.Should().BeNull();
    }

    /// <summary>
    ///     Тест 2: Должен отправлять текст ошибки и оставаться на шаге при невалидном вводе.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен отправлять текст ошибки и оставаться на шаге при невалидном вводе")]
    public async Task Should_SendError_OnInvalidInput()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store, TimeSpan.FromMinutes(1));
        var client = new DummyTransportClient();
        var services = new ServiceCollection().AddSingleton<ITransportClient>(client).BuildServiceProvider();
        var ctx = CreateContext(services);
        var scene = new WizardSceneBuilder()
            .AddStep("имя?", (c, text) => Task.FromResult(text == "иван" ? null : "ошибка"))
            .Build("profile", navigator, client);

        await navigator.EnterAsync(ctx, scene);
        await scene.OnUpdate(ctx with { Text = "пётр" });

        client.SentTexts.Last().text.Should().Be("ошибка");
        var state = await navigator.GetStateAsync(ctx);
        state!.Step.Should().Be(1);
    }

    /// <summary>
    ///     Тест 3: Должен выбрасывать исключение при запуске мастера без шагов.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Должен выбрасывать исключение при запуске мастера без шагов")]
    public async Task Should_Throw_OnEmptyWizard()
    {
        var store = new InMemoryStateStore();
        var navigator = new SceneNavigator(store);
        var client = new DummyTransportClient();
        var services = new ServiceCollection().AddSingleton<ITransportClient>(client).BuildServiceProvider();
        var ctx = CreateContext(services);
        var scene = new WizardSceneBuilder().Build("profile", navigator, client);

        var act = async () => await navigator.EnterAsync(ctx, scene);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private static UpdateContext CreateContext(IServiceProvider services)
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
            services,
            CancellationToken.None);
    }
}
