using System.Net;
using System.Net.Http;

using FluentAssertions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Stalinon.Bot.Hosting.Options;
using Stalinon.Bot.Tests.Shared;

using Telegram.Bot;
using Telegram.Bot.Exceptions;

using Xunit;

namespace Stalinon.Bot.Telegram.Tests;

/// <summary>
///     Тесты сервиса управления вебхуком Telegram: регистрация, удаление и обработка ошибок.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Регистрирует вебхук и пишет лог.</item>
///         <item>Удаляет вебхук и пишет лог.</item>
///         <item>Пробрасывает ошибку при неудачной регистрации.</item>
///         <item>Пробрасывает ошибку при неудачном удалении.</item>
///     </list>
/// </remarks>
public sealed class WebhookServiceTests
{
    /// <inheritdoc />
    public WebhookServiceTests()
    {
    }

    /// <summary>
    ///     Тест 1: Регистрирует вебхук и пишет лог.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Регистрирует вебхук и пишет лог.")]
    public async Task Should_SetWebhook_AndLog()
    {
        // Arrange
        var handler = new TestHandler();
        var client = new TelegramBotClient("1:token", new HttpClient(handler));
        var service = CreateService(client, out var logger);

        // Act
        await service.SetWebhookAsync(CancellationToken.None);

        // Assert
        handler.RequestUri.Should().EndWith("setWebhook");
        handler.Body.Should().Contain("\"url\":\"https://example.com/tg/secret\"");
        logger.Logs.Should().ContainSingle(l => l.Level == LogLevel.Information && l.Message == "webhook set to https://example.com/tg/secret");
    }

    /// <summary>
    ///     Тест 2: Удаляет вебхук и пишет лог.
    /// </summary>
    [Fact(DisplayName = "Тест 2: Удаляет вебхук и пишет лог.")]
    public async Task Should_DeleteWebhook_AndLog()
    {
        // Arrange
        var handler = new TestHandler();
        var client = new TelegramBotClient("1:token", new HttpClient(handler));
        var service = CreateService(client, out var logger);

        // Act
        await service.DeleteWebhookAsync(CancellationToken.None);

        // Assert
        handler.RequestUri.Should().EndWith("deleteWebhook");
        logger.Logs.Should().ContainSingle(l => l.Level == LogLevel.Information && l.Message == "webhook deleted");
    }

    /// <summary>
    ///     Тест 3: Пробрасывает ошибку при неудачной регистрации.
    /// </summary>
    [Fact(DisplayName = "Тест 3: Пробрасывает ошибку при неудачной регистрации.")]
    public async Task Should_Throw_OnSetWebhookError()
    {
        // Arrange
        var handler = new TestHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"ok\":false,\"error_code\":400,\"description\":\"err\"}")
            }
        };
        var client = new TelegramBotClient("1:token", new HttpClient(handler));
        var service = CreateService(client, out _);

        // Act
        var act = async () => await service.SetWebhookAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ApiRequestException>();
    }

    /// <summary>
    ///     Тест 4: Пробрасывает ошибку при неудачном удалении.
    /// </summary>
    [Fact(DisplayName = "Тест 4: Пробрасывает ошибку при неудачном удалении.")]
    public async Task Should_Throw_OnDeleteWebhookError()
    {
        // Arrange
        var handler = new TestHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"ok\":false,\"error_code\":400,\"description\":\"err\"}")
            }
        };
        var client = new TelegramBotClient("1:token", new HttpClient(handler));
        var service = CreateService(client, out _);

        // Act
        var act = async () => await service.DeleteWebhookAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ApiRequestException>();
    }

    private static WebhookService CreateService(ITelegramBotClient client, out CollectingLoggerProvider logger)
    {
        var options = Options.Create(new BotOptions
        {
            Transport = new TransportOptions
            {
                Webhook = new WebhookOptions
                {
                    PublicUrl = "https://example.com/",
                    Secret = "secret"
                }
            }
        });
        var p = new CollectingLoggerProvider();
        logger = p;
        var factory = LoggerFactory.Create(b => b.AddProvider(p));
        var service = new WebhookService(client, options, factory.CreateLogger<WebhookService>());
        return service;
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"ok\":true,\"result\":true}")
        };

        public string RequestUri { get; private set; } = string.Empty;

        public string Body { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri!.AbsolutePath;
            Body = request.Content is null ? string.Empty : request.Content.ReadAsStringAsync(cancellationToken).Result;
            return Task.FromResult(Response);
        }
    }
}

