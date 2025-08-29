using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bot.Telegram;

/// <summary>
///     Сервис получения обновлений через полинг
/// </summary>
public sealed class TelegramPollingSource(ITelegramBotClient client, ILogger<TelegramPollingSource> logger)
    : IUpdateSource
{
    /// <summary>
    ///     Получает обновления через полинг и передает их в обработчик
    /// </summary>
    public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
    {
        await client.DeleteWebhook(cancellationToken: ct);
        var me = await client.GetMe(ct);
        logger.LogInformation("telegram polling started as @{username}", me.Username);
        
        var receiver = new ReceiverOptions
        {
            AllowedUpdates =
            [
                UpdateType.Message, UpdateType.EditedMessage,
                UpdateType.CallbackQuery, UpdateType.MyChatMember
            ]
        };

        client.StartReceiving(async (_, update, token) =>
            {
                try
                {
                    var ctx = TelegramUpdateMapper.Map(update);
                    if (ctx is not null)
                    {
                        await onUpdate(ctx with { Services = default!, CancellationToken = token });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "error in update handler");
                }
            },
            (bot, ex, _) =>
            {
                logger.LogError(ex, "telegram receiver error");
                return Task.CompletedTask;
            }, receiver, ct);


        // Keep running until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, ct);
        } catch (OperationCanceledException) { }
    }
    
}
