using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bot.Telegram;
using FluentAssertions;
using Telegram.Bot;
using Xunit;

namespace Bot.Telegram.Tests;

/// <summary>
///     Тесты отправителя ответов Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется отправка только одного ответа при повторном <c>query_id</c>.</item>
///         <item>Проверяется проброс исключения клиента.</item>
///     </list>
/// </remarks>
public sealed class TelegramWebAppQueryResponderTests
{
    private readonly TestHandler _handler;
    private readonly TelegramWebAppQueryResponder _responder;

    /// <inheritdoc/>
    public TelegramWebAppQueryResponderTests()
    {
        _handler = new TestHandler();
        var client = new TelegramBotClient("123:token", new HttpClient(_handler));
        _responder = new TelegramWebAppQueryResponder(client);
    }

    /// <summary>
    ///     Тест 1: Отправляет ответ только один раз при одинаковом <c>query_id</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Отправляет ответ только один раз при одинаковом query_id.")]
    public async Task Should_SendOnce_OnDuplicateQueryId()
    {
        var first = await _responder.RespondAsync("1", "text", CancellationToken.None);
        var second = await _responder.RespondAsync("1", "text", CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();
        _handler.Calls.Should().Be(1);
    }

    /// <summary>
    ///     Тест 2: Пробрасывает исключение клиента.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Пробрасывает исключение клиента.")]
    public async Task Should_Throw_WhenClientFails()
    {
        _handler.Throw = true;

        var act = () => _responder.RespondAsync("2", "text", CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public bool Throw { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            if (Throw)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"ok\":false,\"error_code\":400,\"description\":\"err\"}")
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"ok\":true,\"result\":{\"inline_message_id\":\"1\"}}")
            });
        }
    }
}

