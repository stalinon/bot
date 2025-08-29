using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Hosting.Options;
using System.Threading.Channels;
using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Options;

namespace Bot.Telegram;

/// <summary>
///     Источник обновлений для телеграм через вебхук
/// </summary>
public sealed class TelegramWebhookSource(
    ITelegramBotClient client,
    IOptions<BotOptions> options)
    : IUpdateSource
{
    private readonly Channel<Update> _updates = Channel.CreateUnbounded<Update>();
    private readonly ITelegramBotClient _client = client;
    private readonly BotOptions _options = options.Value;

    /// <summary>
    ///     Поместить обновление в очередь
    /// </summary>
    public ValueTask Enqueue(Update update) => _updates.Writer.WriteAsync(update);

    /// <summary>
    ///     Читает очередь и передает обновления в обработчик
    /// </summary>
    public async Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_options.Transport.PublicUrl))
        {
            var url = $"{_options.Transport.PublicUrl.TrimEnd('/')}/tg/{_options.Transport.Secret}";
            await _client.SetWebhook(url, cancellationToken: ct);
        }

        await foreach (var update in _updates.Reader.ReadAllAsync(ct))
        {
            var ctx = TelegramUpdateMapper.Map(update);
            if (ctx is not null)
            {
                await onUpdate(ctx with { Services = default!, CancellationToken = ct });
            }
        }
    }
}
