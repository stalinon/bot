using System.Collections.Concurrent;

using Bot.Abstractions.Addresses;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bot.Telegram;

/// <summary>
///	Сервис управления меню чата.
/// </summary>
/// <remarks>
///	<list type="number">
///	    <item>Кэширует текущие настройки.</item>
///	    <item>Устанавливает или обновляет кнопку Web App через API Telegram.</item>
///	</list>
/// </remarks>
public sealed class ChatMenuService(ITelegramBotClient client) : IChatMenuService
{
    private readonly ConcurrentDictionary<long, (string Text, string Url)> _cache = new();

    /// <inheritdoc />
    public async Task SetWebAppMenuAsync(ChatAddress chat, string text, string url, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var desired = (Text: text, Url: url);
        if (_cache.TryGetValue(chat.Id, out var current) && current == desired)
        {
            return;
        }

        var button = new MenuButtonWebApp { Text = text, WebApp = new WebAppInfo { Url = url } };
        await client.SetChatMenuButton(chat.Id, button, cancellationToken: ct);
        _cache[chat.Id] = desired;
    }
}

