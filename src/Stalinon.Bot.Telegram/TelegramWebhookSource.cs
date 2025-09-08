using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Queue;
using Stalinon.Bot.Core.Stats;
using Stalinon.Bot.Hosting.Options;

using Microsoft.Extensions.Options;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace Stalinon.Bot.Telegram;

/// <summary>
///     Источник обновлений для телеграм через вебхук
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Буферизует входящие обновления.</item>
///         <item>Учитывает потерянные обновления при переполнении.</item>
///         <item>Отслеживает текущую глубину очереди.</item>
///     </list>
/// </remarks>
public sealed class TelegramWebhookSource(
    ITelegramBotClient client,
    IOptions<BotOptions> options,
    QueueOptions queueOptions,
    StatsCollector stats)
    : IUpdateSource
{
    private readonly BotOptions _options = options.Value;
    private readonly UpdateQueue<Update> _updates = new(
        options.Value.Transport.Webhook.QueueCapacity,
        queueOptions.Policy,
        stats);

    /// <summary>
    ///     Читает очередь и передает обновления в обработчик
    /// </summary>
    public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_options.Transport.Webhook.PublicUrl))
        {
            var url = $"{_options.Transport.Webhook.PublicUrl.TrimEnd('/')}/tg/{_options.Transport.Webhook.Secret}";
            await client.SetWebhook(url, cancellationToken: ct);
        }

        await foreach (var update in _updates.ReadAllAsync(ct))
        {
            var ctx = TelegramUpdateMapper.Map(update);
            if (ctx is not null)
            {
                await onUpdate(ctx with { Services = default!, CancellationToken = ct });
            }
            stats.SetQueueDepth(_updates.Count);
        }
    }

    /// <summary>
    ///     Остановить получение обновлений через вебхук.
    /// </summary>
    public async Task StopAsync()
    {
        _updates.Complete();
        try
        {
            await client.DeleteWebhook(cancellationToken: CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    /// <summary>
    ///     Попытаться поместить обновление в очередь
    /// </summary>
    /// <returns><c>true</c>, если помещено успешно</returns>
    public ValueTask<bool> TryEnqueueAsync(Update update, CancellationToken ct = default)
    {
        return _updates.EnqueueAsync(update, ct);
    }
}
