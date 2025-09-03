using System.Linq;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Core.Scenes;
using Bot.Examples.HelloBot.Handlers;
using Bot.Examples.HelloBot.Scenes;
using Bot.TestKit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bot.Examples.HelloBot.Tests;

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
    private readonly PhoneHandler _handler;
    private readonly SceneNavigator _navigator;
    private readonly FakeTransportClient _client;

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
        var ctx = CreateCtx("/phone", "/phone");
        await _handler.HandleAsync(ctx);

        var ctx2 = CreateCtx("+79991234567", null);
        await _handler.HandleAsync(ctx2);

        var ctx3 = CreateCtx("да", null);
        await _handler.HandleAsync(ctx3);

        _client.SentTexts.Select(t => t.text).Should().ContainInOrder(
            "введите номер телефона",
            "подтвердите номер: +79991234567 (да/нет)",
            "номер сохранён: +79991234567");

        var state = await _navigator.GetStateAsync(ctx3);
        state.Should().BeNull();
    }

    /// <summary>
    ///     Тест 2: Должен выйти из сцены по таймауту.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Должен выйти из сцены по таймауту")]
    public async Task Should_ExitByTimeout()
    {
        var ctx = CreateCtx("/phone", "/phone");
        await _handler.HandleAsync(ctx);

        await Task.Delay(150);

        var ctx2 = CreateCtx("+79991234567", null);
        await _handler.HandleAsync(ctx2);

        _client.SentTexts[^1].text.Should().Be("не понимаю :(");
        var state = await _navigator.GetStateAsync(ctx2);
        state.Should().BeNull();
    }

    private static UpdateContext CreateCtx(string text, string? command)
    {
        var services = new ServiceCollection().BuildServiceProvider();
        return new UpdateContext(
            "tg",
            "1",
            new ChatAddress(1),
            new UserAddress(1),
            text,
            command,
            null,
            null,
            new Dictionary<string, object>(),
            services,
            CancellationToken.None);
    }
}
