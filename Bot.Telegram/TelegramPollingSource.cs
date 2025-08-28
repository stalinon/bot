using Bot.Abstractions;
using Bot.Abstractions.Addresses;
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
    /// <inheritdoc />
    public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
    {
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

        client.StartReceiving(async (_, update, _) =>
            {
                try
                {
                    var ctx = Map(update);
                    if (ctx is not null)
                    {
                        await onUpdate(ctx with { Services = default! /* will be set by hosted pipeline */ });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "error in update handler");
                }
            },
            (bot, ex, token) =>
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
    
    private static UpdateContext? Map(Update u)
    {
        if (u.Message is { } m)
        {
            var chat = new ChatAddress(m.Chat.Id, m.Chat.Type.ToString());
            var user = m.From is null ? new UserAddress(0) : new UserAddress(m.From.Id, m.From.Username, m.From.LanguageCode);
            var text = m.Type == MessageType.Text ? m.Text : null;
            return new UpdateContext(
                Transport: "telegram",
                UpdateId: u.Id.ToString(),
                Chat: chat,
                User: user,
                Text: text,
                Command: null,
                Payload: null,
                Items: new Dictionary<string, object>(),
                Services: null!,
                CancellationToken: default);
        }
        
        if (u.CallbackQuery is { } cq)
        {
            var chat = new ChatAddress(cq.Message!.Chat.Id, cq.Message.Chat.Type.ToString());
            var user = new UserAddress(cq.From.Id, cq.From.Username, cq.From.LanguageCode);
            return new UpdateContext(
                "telegram", u.Id.ToString(), chat, user,
                Text: null,
                Command: null,
                Payload: cq.Data,
                Items: new Dictionary<string, object>(),
                Services: null!,
                CancellationToken: default);
        }
        
        return null;
    }
}
