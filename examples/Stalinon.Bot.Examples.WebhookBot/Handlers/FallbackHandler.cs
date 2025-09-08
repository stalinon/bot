using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Examples.WebhookBot.Handlers;

/// <summary>
///     Обработчик на неизвестный ввод
/// </summary>
public sealed class FallbackHandler(ITransportClient tx) : IFallbackHandler
{
    /// <inheritdoc />
    public Task HandleAsync(UpdateContext ctx)
    {
        return tx.SendTextAsync(ctx.Chat, "не понимаю :(", ctx.CancellationToken);
    }
}
