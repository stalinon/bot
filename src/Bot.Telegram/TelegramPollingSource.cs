using System.Threading.Channels;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;

using Microsoft.Extensions.Logging;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Bot.Telegram;

/// <summary>
///     Сервис получения обновлений через поллинг
/// </summary>
public sealed class TelegramPollingSource(ITelegramBotClient client, ILogger<TelegramPollingSource> logger)
    : IUpdateSource
{
    private readonly Channel<Update> _updates = Channel.CreateBounded<Update>(
        new BoundedChannelOptions(1024)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

    /// <summary>
    ///     Получает обновления через поллинг и передает их в обработчик
    /// </summary>
    public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
    {
        await client.DeleteWebhook(cancellationToken: ct);
        var me = await client.GetMe(ct);
        logger.LogInformation("telegram polling started as @{username}", me.Username);

        var polling = Task.Run(async () =>
        {
            var offset = 0;
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var updates = await client.GetUpdates(offset, cancellationToken: ct);
                    foreach (var update in updates)
                    {
                        offset = update.Id + 1;
                        await _updates.Writer.WriteAsync(update, ct);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _updates.Writer.TryComplete();
            }
        }, CancellationToken.None);

        var processing = Task.Run(async () =>
        {
            await foreach (var update in _updates.Reader.ReadAllAsync())
            {
                try
                {
                    var ctx = TelegramUpdateMapper.Map(update);
                    if (ctx is not null)
                    {
                        await onUpdate(ctx with { Services = default!, CancellationToken = ct });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "error in update handler");
                }
            }
        }, CancellationToken.None);

        await Task.WhenAll(polling, processing);
    }
}
