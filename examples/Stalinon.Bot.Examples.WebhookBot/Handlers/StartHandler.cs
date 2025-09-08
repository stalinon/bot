using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Examples.WebhookBot.Handlers;

/// <summary>
///     Запуск
/// </summary>
[Command("/start")]
public sealed class StartHandler(ITransportClient tx) : IUpdateHandler
{
    /// <inheritdoc />
    public Task HandleAsync(UpdateContext ctx)
    {
        return tx.SendTextAsync(ctx.Chat, "привет. я живой. напиши /ping", ctx.CancellationToken);
    }
}
