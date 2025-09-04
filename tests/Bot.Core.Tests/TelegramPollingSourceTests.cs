using Bot.Telegram;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Moq;

using Telegram.Bot;
using Telegram.Bot.Requests;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using Xunit;

namespace Bot.Core.Tests;

/// <summary>
///     Тесты источника обновлений поллинга телеграм
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Дренаж очереди при остановке</item>
///     </list>
/// </remarks>
public sealed class TelegramPollingSourceTests
{
    /// <summary>
    ///     Тест 1: Должен дождаться обработки оставшихся обновлений при остановке
    /// </summary>
    [Fact(DisplayName = "Тест 1: Должен дождаться обработки оставшихся обновлений при остановке")]
    public async Task Should_Drain_RemainingUpdates_OnStop()
    {
        var updates = new[]
        {
            new Update
            {
                Id = 1,
                Message = new Message
                {
                    Date = DateTime.UnixEpoch,
                    Chat = new Chat { Id = 1, Type = ChatType.Private },
                    From = new User { Id = 1 },
                    Text = "1"
                }
            },
            new Update
            {
                Id = 2,
                Message = new Message
                {
                    Date = DateTime.UnixEpoch,
                    Chat = new Chat { Id = 1, Type = ChatType.Private },
                    From = new User { Id = 1 },
                    Text = "2"
                }
            }
        };

        var client = new Mock<ITelegramBotClient>();
        client.Setup(x => x.SendRequest(It.IsAny<DeleteWebhookRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        client.Setup(x => x.SendRequest(It.IsAny<GetMeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Username = "bot" });

        var firstCall = true;
        client.Setup(x => x.SendRequest(It.IsAny<GetUpdatesRequest>(), It.IsAny<CancellationToken>()))
            .Returns(async (GetUpdatesRequest req, CancellationToken token) =>
            {
                if (firstCall)
                {
                    firstCall = false;
                    return updates;
                }

                try
                {
                    await Task.Delay(Timeout.Infinite, token);
                }
                catch (OperationCanceledException)
                {
                }

                return Array.Empty<Update>();
            });

        var processed = new List<string>();
        var logger = new LoggerFactory().CreateLogger<TelegramPollingSource>();
        var source = new TelegramPollingSource(client.Object, logger);
        using var cts = new CancellationTokenSource();

        var start = source.StartAsync(async ctx =>
        {
            processed.Add(ctx.UpdateId);
            if (processed.Count == 1)
            {
                await Task.Delay(100, ctx.CancellationToken);
            }
        }, cts.Token);

        await Task.Delay(10);
        cts.Cancel();
        await start; // wait for draining

        processed.Should().Equal("1", "2");
    }
}
