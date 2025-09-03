using Bot.Hosting.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Telegram.Bot;

namespace Bot.Telegram;

/// <summary>
///     Сервис управления вебхуком телеги.
/// </summary>
public sealed class WebhookService
{
    private readonly ITelegramBotClient _client;
    private readonly BotOptions _options;
    private readonly ILogger<WebhookService> _logger;

    /// <summary>
    ///     Инициализировать сервис.
    /// </summary>
    public WebhookService(ITelegramBotClient client, IOptions<BotOptions> options, ILogger<WebhookService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    ///     Зарегистрировать вебхук.
    /// </summary>
    public async Task SetWebhookAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var url = $"{_options.Transport.Webhook.PublicUrl?.TrimEnd('/')}/tg/{_options.Transport.Webhook.Secret}";
        await _client.SetWebhook(url, cancellationToken: ct);
        _logger.LogInformation("webhook set to {Url}", url);
    }

    /// <summary>
    ///     Удалить вебхук.
    /// </summary>
    public async Task DeleteWebhookAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _client.DeleteWebhook(cancellationToken: ct);
        _logger.LogInformation("webhook deleted");
    }
}
