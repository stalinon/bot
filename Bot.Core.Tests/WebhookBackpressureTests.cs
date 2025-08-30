using System.Net;
using System.Net.Http.Json;
using Bot.Hosting.Options;
using Bot.Telegram;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты механизма обратного давления вебхука.
/// </summary>
public class WebhookBackpressureTests
{
    /// <summary>
    ///     Проверяет, что при переполнении очереди возвращается 429 и пишется лог.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Переполнение очереди вебхука даёт 429")]
    public async Task Overflow_returns_429_and_logs()
    {
        var provider = new CollectingLoggerProvider();
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddSingleton<IOptions<BotOptions>>(Microsoft.Extensions.Options.Options.Create(new BotOptions
                {
                    Transport = new TransportOptions { Secret = "s", QueueCapacity = 1 }
                }));
                services.AddSingleton<ITelegramBotClient>(Mock.Of<ITelegramBotClient>());
                services.AddSingleton<TelegramWebhookSource>();
                services.AddLogging(b => b.AddProvider(provider));
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(e => e.MapTelegramWebhook());
            });

        using var server = new TestServer(builder);
        var client = server.CreateClient();

        var content1 = JsonContent.Create(new { update_id = 1 });
        var resp1 = await client.PostAsync("/tg/s", content1);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        var content2 = JsonContent.Create(new { update_id = 2 });
        var resp2 = await client.PostAsync("/tg/s", content2);
        Assert.Equal(HttpStatusCode.TooManyRequests, resp2.StatusCode);
        Assert.Contains(provider.Logs, l => l.Message.Contains("webhook queue overflow"));
    }
}
