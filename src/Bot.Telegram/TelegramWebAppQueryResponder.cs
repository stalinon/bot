using Bot.Abstractions.Contracts;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Telegram.Bot;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

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
public sealed class TelegramWebAppQueryResponder : IWebAppQueryResponder
{
    private readonly ITelegramBotClient _client;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TelegramWebAppQueryResponder> _logger;
    private readonly TimeSpan _ttl;

    public TelegramWebAppQueryResponder(
        ITelegramBotClient client,
        IMemoryCache cache,
        ILogger<TelegramWebAppQueryResponder> logger,
        TimeSpan? ttl = null)
    {
        _client = client;
        _cache = cache;
        _logger = logger;
        _ttl = ttl ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    public async Task<bool> RespondAsync(string queryId, string text, CancellationToken ct)
    {
        if (_cache.TryGetValue(queryId, out _))
        {
            return false;
        }

        try
        {
            ct.ThrowIfCancellationRequested();
            var content = new InputTextMessageContent(text);
            var article = new InlineQueryResultArticle(queryId, text, content);
            await _client.AnswerWebAppQuery(queryId, article, cancellationToken: ct);
            _cache.Set(queryId, true, _ttl);
            return true;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Тайм-аут ответа Web App для query_id {QueryId}", queryId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка ответа Web App для query_id {QueryId}", queryId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RespondWithImageAsync(string queryId, string url, CancellationToken ct)
    {
        if (_cache.TryGetValue(queryId, out _))
        {
            return false;
        }

        try
        {
            ct.ThrowIfCancellationRequested();
            var photo = new InlineQueryResultPhoto(queryId, url, url);
            await _client.AnswerWebAppQuery(queryId, photo, cancellationToken: ct);
            _cache.Set(queryId, true, _ttl);
            return true;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Тайм-аут ответа Web App для query_id {QueryId}", queryId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка ответа Web App для query_id {QueryId}", queryId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RespondWithButtonAsync(string queryId, string text, string buttonText, string buttonUrl, CancellationToken ct)
    {
        if (_cache.TryGetValue(queryId, out _))
        {
            return false;
        }

        try
        {
            ct.ThrowIfCancellationRequested();
            var content = new InputTextMessageContent(text);
            var markup = new InlineKeyboardMarkup(InlineKeyboardButton.WithUrl(buttonText, buttonUrl));
            var article = new InlineQueryResultArticle(queryId, text, content)
            {
                ReplyMarkup = markup,
            };
            await _client.AnswerWebAppQuery(queryId, article, cancellationToken: ct);
            _cache.Set(queryId, true, _ttl);
            return true;
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Тайм-аут ответа Web App для query_id {QueryId}", queryId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка ответа Web App для query_id {QueryId}", queryId);
            return false;
        }
    }
}
