using Bot.Abstractions;
using Bot.Abstractions.Contracts;

namespace Bot.Examples.HelloBot.Handlers;

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
