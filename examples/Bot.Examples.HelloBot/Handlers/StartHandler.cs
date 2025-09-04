using Bot.Abstractions;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;

namespace Bot.Examples.HelloBot.Handlers;

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
