using System;
using System.Threading;
using System.Threading.Tasks;
using Bot.Telegram;
using Bot.Abstractions;
using Telegram.Bot.Types;
using System.Text.Json;
using Bot.Hosting.Options;
using Microsoft.Extensions.Options;
using Moq;
using Telegram.Bot;
using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты источника обновлений вебхука телеграм
/// </summary>
public class TelegramWebhookSourceTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///     Проверяет, что отправленное обновление доходит до обработчика
    /// </summary>
    [Fact(DisplayName = "Тест 1. Обновление из вебхука поступает обработчику")]
    public async Task EnqueuedUpdate_Reaches_Handler()
    {
        var bot = Mock.Of<ITelegramBotClient>();
        var options = Microsoft.Extensions.Options.Options.Create(new BotOptions { Transport = new TransportOptions() });
        var source = new TelegramWebhookSource(bot, options);
        var tcs = new TaskCompletionSource<UpdateContext>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var _ = source.StartAsync(ctx =>
        {
            tcs.TrySetResult(ctx);
            return Task.CompletedTask;
        }, cts.Token);

        var obj = new
        {
            update_id = 1,
            message = new
            {
                message_id = 2,
                date = 0,
                chat = new { id = 3, type = "private" },
                from = new { id = 4 },
                text = "hi"
            }
        };
        var json = JsonSerializer.Serialize(obj, Json);
        var upd = JsonSerializer.Deserialize<Update>(json, Json)!;

        await source.Enqueue(upd);

        var ctx = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Equal("1", ctx.UpdateId);
    }
}
