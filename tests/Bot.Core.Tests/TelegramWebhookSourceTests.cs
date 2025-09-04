using System.Text.Json;

using Bot.Abstractions;
using Bot.Core.Stats;
using Bot.Hosting.Options;
using Bot.Telegram;

using Moq;

using Telegram.Bot;
using Telegram.Bot.Types;

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
        var options =
            Microsoft.Extensions.Options.Options.Create(new BotOptions { Transport = new TransportOptions() });
        var source = new TelegramWebhookSource(bot, options, new StatsCollector());
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

        var enqueued = source.TryEnqueue(upd);
        Assert.True(enqueued);

        var ctx = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.Equal("1", ctx.UpdateId);
    }
}
