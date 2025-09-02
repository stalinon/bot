using Bot.Abstractions;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;

namespace Bot.Examples.WebhookBot.Handlers;

/// <summary>
///     Запуск
/// </summary>
[Command("/start")]
public sealed class StartHandler(ITransportClient tx) : IUpdateHandler
{
    /// <inheritdoc />
    public Task HandleAsync(UpdateContext ctx)
        => tx.SendTextAsync(ctx.Chat, "привет. я живой. напиши /ping", ctx.CancellationToken);
}