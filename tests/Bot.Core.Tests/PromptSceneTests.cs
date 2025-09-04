using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;
using Bot.Core.Scenes;
using Bot.TestKit;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты вспомогательной сцены с вопросом и валидатором.
/// </summary>
public sealed class PromptSceneTests
{
    /// <summary>
    ///     Тест 1. Проверяем успешный сбор и валидацию поля.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Проверяем успешный сбор и валидацию поля")]
    public async Task PromptSceneValid()
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
        string? result = null;
        var scene = new PromptScene(
            navigator,
            "prompt",
            "Введите число",
            text => int.TryParse(text, out _),
            (c, value) =>
            {
                result = value;
                return Task.CompletedTask;
            });

        await navigator.EnterAsync(ctx, scene);
        Assert.Single(client.SentTexts);

        var ctx2 = ctx with { Text = "123" };
        await scene.OnUpdate(ctx2);

        Assert.Equal("123", result);
        var state = await navigator.GetStateAsync(ctx);
        Assert.Null(state);
    }

    /// <summary>
    ///     Тест 2. Проверяем обработку некорректного ввода.
    /// </summary>
    [Fact(DisplayName = "Тест 2. Проверяем обработку некорректного ввода")]
    public async Task PromptSceneInvalid()
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
        var scene = new PromptScene(
            navigator,
            "prompt",
            "Введите число",
            text => int.TryParse(text, out _),
            (c, value) => Task.CompletedTask,
            "Ошибка");

        await navigator.EnterAsync(ctx, scene);
        var ctx2 = ctx with { Text = "abc" };
        await scene.OnUpdate(ctx2);

        Assert.Equal("Ошибка", client.SentTexts[^1].text);
        var state = await navigator.GetStateAsync(ctx);
        Assert.NotNull(state);
    }
}
