using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Telegram;

/// <summary>
///     UI для Telegram
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Добавляет кнопку Web App в меню.</item>
///         <item>Отправляет сообщение с inline-кнопкой Web App.</item>
///         <item>Отправляет сообщение с reply-кнопкой Web App.</item>
///     </list>
/// </remarks>
public sealed class TelegramBotUi(ITelegramBotClient client) : IBotUi
{
    /// <inheritdoc />
    public Task SendMenuWebAppAsync(ChatAddress chat, string text, string url, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var button = new MenuButtonWebApp { Text = text, WebApp = new WebAppInfo { Url = url } };
        return client.SetChatMenuButton(chat.Id, button, cancellationToken: ct);
    }

    /// <inheritdoc />
    public Task SendInlineWebAppAsync(ChatAddress chat, string text, string url, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var markup = new InlineKeyboardMarkup(InlineKeyboardButton.WithWebApp(text, new WebAppInfo { Url = url }));
        return client.SendMessage(chat.Id, text, replyMarkup: markup, cancellationToken: ct);
    }

    /// <inheritdoc />
    public Task SendReplyWebAppAsync(ChatAddress chat, string text, string url, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var markup = new ReplyKeyboardMarkup(KeyboardButton.WithWebApp(text, new WebAppInfo { Url = url }))
        {
            ResizeKeyboard = true,
        };
        return client.SendMessage(chat.Id, text, replyMarkup: markup, cancellationToken: ct);
    }
}
