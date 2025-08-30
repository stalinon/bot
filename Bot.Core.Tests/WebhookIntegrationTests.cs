using System;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions;
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
        var srcOptions = Options.Create(new BotOptions
        {
            Transport = new TransportOptions { QueueCapacity = 8 }
        });
        var source = new TelegramWebhookSource(Mock.Of<ITelegramBotClient>(), srcOptions);

        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddLogging();
                services.AddSingleton<IOptions<BotOptions>>(Options.Create(new BotOptions
                {
                    Transport = new TransportOptions { Secret = "s", QueueCapacity = 8 }
                }));
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
        var resp = await client.PostAsJsonAsync("/tg/s", new { update_id = 1 });
        Assert.True(resp.IsSuccessStatusCode);

        await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(tcs.Task.IsCompleted);

        cts.Cancel();
    }
}

