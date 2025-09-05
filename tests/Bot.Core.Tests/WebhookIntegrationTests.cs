using System.Net.Http.Json;

using Bot.Abstractions;
using Bot.Core.Options;
using Bot.Core.Stats;
using Bot.Hosting.Options;
using Bot.Telegram;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Интеграционные тесты телеграм вебхука.
/// </summary>
public sealed class WebhookIntegrationTests
{
    /// <summary>
    ///     Вебхук передаёт обновление обработчику.
    /// </summary>
    [Fact(DisplayName = "Тест 1. Вебхук передаёт обновление обработчику")]
    public async Task Webhook_delivers_update()
    {
        var tcs = new TaskCompletionSource<UpdateContext>();
        var srcOptions = Microsoft.Extensions.Options.Options.Create(new BotOptions
        {
            Transport = new TransportOptions
            {
                Mode = TransportMode.Webhook,
                Webhook = new WebhookOptions { QueueCapacity = 8 }
            }
        });
        var source = new TelegramWebhookSource(
            Mock.Of<ITelegramBotClient>(),
            srcOptions,
            new QueueOptions { Policy = QueuePolicy.Drop },
            new StatsCollector());

        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddLogging();
                services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new BotOptions
                {
                    Transport = new TransportOptions
                    {
                        Mode = TransportMode.Webhook,
                        Webhook = new WebhookOptions { Secret = "s", QueueCapacity = 8 }
                    }
                }));
                services.AddSingleton(new QueueOptions { Policy = QueuePolicy.Drop });
                services.AddSingleton(source);
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseEndpoints(e => e.MapTelegramWebhook());
            });

        using var server = new TestServer(builder);
        using var cts = new CancellationTokenSource();
        _ = source.StartAsync(ctx =>
        {
            tcs.TrySetResult(ctx);
            return Task.CompletedTask;
        }, cts.Token);

        var client = server.CreateClient();
        var resp = await client.PostAsJsonAsync("/tg/s", new Update
        {
            Id = 1,
            Message = new Message
            {
                Chat = new Chat { Id = 1, Type = ChatType.Private },
                From = new User { Username = "", LanguageCode = "" }
            }
        });
        Assert.True(resp.IsSuccessStatusCode);

        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(tcs.Task.IsCompleted);

        cts.Cancel();
    }
}
