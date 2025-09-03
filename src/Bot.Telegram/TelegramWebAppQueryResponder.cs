using System.Collections.Concurrent;
using Bot.Abstractions.Contracts;
using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;

namespace Bot.Telegram;

/// <summary>
///     Отправитель ответов на запросы Web App.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Вызывает метод Telegram API <c>AnswerWebAppQuery</c>.</item>
///         <item>Гарантирует идемпотентность по идентификатору запроса.</item>
///     </list>
/// </remarks>
public sealed class TelegramWebAppQueryResponder(ITelegramBotClient client) : IWebAppQueryResponder
{
    private readonly ConcurrentDictionary<string, byte> _answered = new();

    /// <inheritdoc />
    public async Task<bool> RespondAsync(string queryId, string text, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!_answered.TryAdd(queryId, 0))
        {
            return false;
        }

        var content = new InputTextMessageContent(text);
        var article = new InlineQueryResultArticle(queryId, text, content);
        await client.AnswerWebAppQuery(queryId, article, cancellationToken: ct);
        return true;
    }
}

