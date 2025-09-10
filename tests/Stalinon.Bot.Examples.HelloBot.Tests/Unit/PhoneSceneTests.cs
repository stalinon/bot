using FluentAssertions;

using Stalinon.Bot.Core.Scenes;
using Stalinon.Bot.Examples.HelloBot.Handlers;
using Stalinon.Bot.Examples.HelloBot.Scenes;
using Stalinon.Bot.TestKit;

using Xunit;

namespace Stalinon.Bot.Examples.HelloBot.Tests;

/// <summary>
///     Тесты сцены ввода телефона: успешный сценарий и выход по таймауту.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется успешный ввод и подтверждение номера.</item>
///         <item>Проверяется автоматический выход при истечении TTL.</item>
///     </list>
/// </remarks>
public sealed class PhoneSceneTests
{
    private readonly FakeTransportClient _client;
    private readonly PhoneHandler _handler;
    private readonly SceneNavigator _navigator;

    /// <inheritdoc />
    public PhoneSceneTests()
    {
        var store = new InMemoryStateStore();
        _navigator = new SceneNavigator(store, TimeSpan.FromMilliseconds(100));
        _client = new FakeTransportClient();
        var scene = new PhoneScene(_client, _navigator, TimeSpan.FromMilliseconds(100));
        var fallback = new FallbackHandler(_client);
        _handler = new PhoneHandler(_navigator, scene, fallback);
    }

    /// <summary>
    ///     Тест 1: Должен успешно запросить и подтвердить номер.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен успешно запросить и подтвердить номер")]
    public async Task Should_AskAndConfirmPhone()
    {
        var ctx = SceneTestExtensions.CreateContext("/phone", "phone");
        await _handler.HandleAsync(ctx);

        await _handler.StepAsync(_navigator, ctx, "+79991234567");
        var state = await _handler.StepAsync(_navigator, ctx, "да");

        _client.SentTexts.Select(t => t.text).Should().ContainInOrder(
            "введите номер телефона",
            "подтвердите номер: +79991234567 (да/нет)",
            "номер сохранён: +79991234567");

        state.Should().BeNull();
    }

    /// <summary>
    ///     Тест 2: Должен выйти из сцены по таймауту.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен выйти из сцены по таймауту")]
    public async Task Should_ExitByTimeout()
    {
        var ctx = SceneTestExtensions.CreateContext("/phone", "phone");
        await _handler.HandleAsync(ctx);

        await _handler.StepAsync(_navigator, ctx, "+79991234567", delay: TimeSpan.FromMilliseconds(150));

        _client.SentTexts[^1].text.Should().Be("не понимаю :(");
        var state = await _navigator.GetStateAsync(ctx);
        state.Should().BeNull();
    }
}
