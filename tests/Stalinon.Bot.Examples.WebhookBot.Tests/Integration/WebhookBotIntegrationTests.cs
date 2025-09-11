using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.TestKit;

using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using Xunit;

namespace Stalinon.Bot.Examples.WebhookBot.Tests.Integration;

/// <summary>
///     Интеграционные тесты WebhookBot: обработка команд через вебхук.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Обрабатывает <c>/start</c> и <c>/ping</c> через POST <c>/tg/secret</c>.</item>
///     </list>
/// </remarks>
public sealed class WebhookBotIntegrationTests : IClassFixture<WebhookBotFactory>
{
    private readonly WebhookBotFactory _factory;

    /// <inheritdoc />
    public WebhookBotIntegrationTests(WebhookBotFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    ///     Тест 1: Должен обрабатывать команды через вебхук.
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен обрабатывать команды через вебхук.")]
    public async Task Should_HandleCommands_ViaWebhook()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        await client.PostAsJsonAsync("/tg/secret", BuildUpdate("/start", 1));
        await client.PostAsJsonAsync("/tg/secret", BuildUpdate("/ping", 2));

        var tx = (FakeTransportClient)_factory.Services.GetRequiredService<ITransportClient>();
        await WaitAsync(() => tx.SentTexts.Count >= 2, TimeSpan.FromSeconds(5));

        tx.SentTexts[0].text.Should().Be("привет. я живой. напиши /ping");
        tx.SentTexts[1].text.Should().StartWith("pong #1");
    }

    private static Update BuildUpdate(string text, int id)
    {
        return new Update
        {
            Id = id,
            Message = new Message
            {
                 Text = text,
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                From = new User { Id = 1, LanguageCode = "ru" }
            }
        };
    }

    private static async Task WaitAsync(Func<bool> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (!condition())
        {
            if (DateTime.UtcNow - start > timeout)
            {
                throw new TimeoutException();
            }

            await Task.Delay(50);
        }
    }
}
