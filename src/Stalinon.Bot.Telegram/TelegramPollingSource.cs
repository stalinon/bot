using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Options;
using Stalinon.Bot.Core.Queue;
using Stalinon.Bot.Core.Stats;

using Microsoft.Extensions.Logging;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace Stalinon.Bot.Telegram;

/// <summary>
///     Сервис получения обновлений через поллинг
/// </summary>
public sealed class TelegramPollingSource(
    ITelegramBotClient client,
    ILogger<TelegramPollingSource> logger,
    QueueOptions queueOptions,
    StatsCollector stats)
    : IUpdateSource
{
    private readonly UpdateQueue<Update> _updates = new(1024, queueOptions.Policy, stats);
    private CancellationTokenSource? _cts;

    /// <summary>
    ///     Получает обновления через поллинг и передает их в обработчик
    /// </summary>
    public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await client.DeleteWebhook(cancellationToken: _cts.Token);
        var me = await client.GetMe(ct);
        logger.LogInformation("telegram polling started as @{username}", me.Username);

        var polling = Task.Run(async () =>
        {
            var offset = 0;
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    var updates = await client.GetUpdates(offset, cancellationToken: _cts.Token);
                    foreach (var update in updates)
                    {
                        offset = update.Id + 1;
                        await _updates.EnqueueAsync(update, _cts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _updates.Complete();
            }
        }, CancellationToken.None);

        var processing = Task.Run(async () =>
        {
            await foreach (var update in _updates.ReadAllAsync(CancellationToken.None))
            {
                try
                {
                    var ctx = TelegramUpdateMapper.Map(update);
                    if (ctx is not null)
                    {
                        await onUpdate(ctx with { Services = default!, CancellationToken = _cts.Token });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "error in update handler");
                }
                finally
                {
                    stats.SetQueueDepth(_updates.Count);
                }
            }
        }, CancellationToken.None);

        await Task.WhenAll(polling, processing);
    }

    /// <summary>
    ///     Остановить источник поллинга.
    /// </summary>
    public Task StopAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}
