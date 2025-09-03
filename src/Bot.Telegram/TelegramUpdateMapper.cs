using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bot.Telegram;

/// <summary>
///     Преобразование обновлений телеграм в контекст
/// </summary>
internal static class TelegramUpdateMapper
{
    /// <summary>
    ///     Преобразовать обновление телеграм в <see cref="UpdateContext"/>
    /// </summary>
    public static UpdateContext? Map(Update u)
    {
        if (u.Message is { } m)
        {
            var chat = new ChatAddress(m.Chat.Id, m.Chat.Type.ToString());
            var user = m.From is null ? new UserAddress(0) : new UserAddress(m.From.Id, m.From.Username, m.From.LanguageCode);
            var text = m.Type == MessageType.Text ? m.Text : null;
            var items = new Dictionary<string, object>
            {
                [UpdateItems.UpdateType] = u.Type.ToString(),
                [UpdateItems.MessageId] = m.MessageId
            };
            var payload = m.WebAppData?.Data;
            if (m.WebAppData is not null)
            {
                items[UpdateItems.WebAppData] = true;
            }

            return new UpdateContext(
                Transport: "telegram",
                UpdateId: u.Id.ToString(),
                Chat: chat,
                User: user,
                Text: text,
                Command: null,
                Args: null,
                Payload: payload,
                Items: items,
                Services: null!,
                CancellationToken: default);
        }

        if (u.CallbackQuery is { } cq)
        {
            var chat = new ChatAddress(cq.Message!.Chat.Id, cq.Message.Chat.Type.ToString());
            var user = new UserAddress(cq.From.Id, cq.From.Username, cq.From.LanguageCode);
            var items = new Dictionary<string, object>
            {
                [UpdateItems.UpdateType] = u.Type.ToString(),
                [UpdateItems.MessageId] = cq.Message!.MessageId
            };
            return new UpdateContext(
                "telegram", u.Id.ToString(), chat, user,
                Text: null,
                Command: null,
                Args: null,
                Payload: cq.Data,
                Items: items,
                Services: null!,
                CancellationToken: default);
        }

        return null;
    }
}
