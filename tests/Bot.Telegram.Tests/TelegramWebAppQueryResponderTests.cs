using System.Net;

using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Telegram.Bot;

using Xunit;

namespace Bot.Telegram.Tests;

/// <summary>
///     Тесты отправителя ответов Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Проверяется отправка только одного ответа при повторном <c>query_id</c>.</item>
///         <item>Проверяется возврат ошибки и логирование при сбоях клиента и тайм-аутах.</item>
///         <item>Проверяется отправка изображений и кнопок.</item>
///         <item>Проверяется повторная отправка после истечения TTL.</item>
///     </list>
/// </remarks>
public sealed class TelegramWebAppQueryResponderTests
{
    /// <inheritdoc />
    public TelegramWebAppQueryResponderTests()
    {
    }

    /// <summary>
    ///     Тест 1: Отправляет текст только один раз при одинаковом <c>query_id</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Отправляет текст только один раз при одинаковом query_id.")]
    public async Task Should_SendTextOnce_OnDuplicateQueryId()
    {
        var (responder, handler, _) = CreateResponder();

        var first = await responder.RespondAsync("1", "text", CancellationToken.None);
        var second = await responder.RespondAsync("1", "text", CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();
        handler.Calls.Should().Be(1);
    }

    /// <summary>
    ///     Тест 2: Возвращает false и пишет в лог при ошибке клиента.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Возвращает false и пишет в лог при ошибке клиента.")]
    public async Task Should_ReturnFalseAndLog_WhenClientFails()
    {
        var (responder, handler, logger) = CreateResponder();
        handler.Throw = true;

        var result = await responder.RespondAsync("2", "text", CancellationToken.None);

        result.Should().BeFalse();
        logger.Logs.Should().Contain(x => x.Level == LogLevel.Error);
    }

    /// <summary>
    ///     Тест 3: Возвращает false и пишет в лог при тайм-ауте клиента.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Возвращает false и пишет в лог при тайм-ауте клиента.")]
    public async Task Should_ReturnFalseAndLog_WhenTimeout()
    {
        var (responder, handler, logger) = CreateResponder();
        handler.Timeout = true;

        var result = await responder.RespondAsync("3", "text", CancellationToken.None);

        result.Should().BeFalse();
        logger.Logs.Should().Contain(x => x.Level == LogLevel.Error);
    }

    /// <summary>
    ///     Тест 4: Отправляет изображение только один раз при одинаковом <c>query_id</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Отправляет изображение только один раз при одинаковом query_id.")]
    public async Task Should_SendImageOnce_OnDuplicateQueryId()
    {
        var (responder, handler, _) = CreateResponder();

        var first = await responder.RespondWithImageAsync("4", "http://img", CancellationToken.None);
        var second = await responder.RespondWithImageAsync("4", "http://img", CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();
        handler.Calls.Should().Be(1);
    }

    /// <summary>
    ///     Тест 5: Отправляет текст с кнопкой только один раз при одинаковом <c>query_id</c>.
    /// </summary>
    [Fact(DisplayName = "Тест 5: Отправляет текст с кнопкой только один раз при одинаковом query_id.")]
    public async Task Should_SendButtonOnce_OnDuplicateQueryId()
    {
        var (responder, handler, _) = CreateResponder();

        var first = await responder.RespondWithButtonAsync("5", "text", "btn", "http://url", CancellationToken.None);
        var second = await responder.RespondWithButtonAsync("5", "text", "btn", "http://url", CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeFalse();
        handler.Calls.Should().Be(1);
    }

    /// <summary>
    ///     Тест 6: Повторно отправляет текст после истечения TTL.
    /// </summary>
    [Fact(DisplayName = "Тест 6: Повторно отправляет текст после истечения TTL.")]
    public async Task Should_ResendText_AfterTtlExpires()
    {
        var (responder, handler, _) = CreateResponder(TimeSpan.FromMilliseconds(50));

        var first = await responder.RespondAsync("6", "text", CancellationToken.None);
        await Task.Delay(60);
        var second = await responder.RespondAsync("6", "text", CancellationToken.None);

        first.Should().BeTrue();
        second.Should().BeTrue();
        handler.Calls.Should().Be(2);
    }

    private static (TelegramWebAppQueryResponder responder, TestHandler handler, CollectingLoggerProvider logger)
        CreateResponder(TimeSpan? ttl = null)
    {
        var handler = new TestHandler();
        var client = new TelegramBotClient("123:token", new HttpClient(handler));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = new CollectingLoggerProvider();
        var factory = LoggerFactory.Create(b => b.AddProvider(provider));
        var responder =
            new TelegramWebAppQueryResponder(client, cache, factory.CreateLogger<TelegramWebAppQueryResponder>(), ttl);
        return (responder, handler, provider);
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public bool Throw { get; set; }
        public bool Timeout { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Calls++;
            if (Timeout)
            {
                return Task.FromException<HttpResponseMessage>(new TaskCanceledException());
            }

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
