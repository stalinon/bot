using System.Net;

using Bot.Abstractions.Addresses;

using FluentAssertions;

using Telegram.Bot;

using Xunit;

namespace Bot.Telegram.Tests;

/// <summary>
///     Тесты сервиса меню чата.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется вызов API при новых настройках.</item>
///         <item>Проверяется отсутствие вызова при повторных одинаковых настройках.</item>
///         <item>Проверяется вызов при изменении настроек.</item>
///     </list>
/// </remarks>
public sealed class ChatMenuServiceTests
{
    private readonly TestHandler _handler;
    private readonly ChatMenuService _service;

    /// <inheritdoc />
    public ChatMenuServiceTests()
    {
        _handler = new TestHandler();
        var client = new TelegramBotClient("1:token", new HttpClient(_handler));
        _service = new ChatMenuService(client);
    }

    /// <summary>
    ///     Тест 1: Вызывает API при установке новых настроек.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Вызывает API при установке новых настроек.")]
    public async Task Should_CallApi_OnNewSettings()
    {
        var chat = new ChatAddress(1);

        await _service.SetWebAppMenuAsync(chat, "t", "u", CancellationToken.None);

        _handler.Calls.Should().Be(1);
    }

    /// <summary>
    ///     Тест 2: Не вызывает API при повторном вызове с теми же настройками.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Не вызывает API при повторном вызове с теми же настройками.")]
    public async Task Should_NotCallApi_OnSameSettings()
    {
        var chat = new ChatAddress(1);

        await _service.SetWebAppMenuAsync(chat, "t", "u", CancellationToken.None);
        await _service.SetWebAppMenuAsync(chat, "t", "u", CancellationToken.None);

        _handler.Calls.Should().Be(1);
    }

    /// <summary>
    ///     Тест 3: Вызывает API при изменении настроек.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Вызывает API при изменении настроек.")]
    public async Task Should_CallApi_OnSettingsChange()
    {
        var chat = new ChatAddress(1);

        await _service.SetWebAppMenuAsync(chat, "t", "u", CancellationToken.None);
        await _service.SetWebAppMenuAsync(chat, "t", "u2", CancellationToken.None);

        _handler.Calls.Should().Be(2);
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true,\"result\":true}")
            });
        }
    }
}
