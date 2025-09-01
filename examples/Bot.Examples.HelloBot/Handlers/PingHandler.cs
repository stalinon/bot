using Bot.Abstractions;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;
using Bot.Examples.HelloBot.Services;

namespace Bot.Examples.HelloBot.Handlers;

/// <summary>
///     Пинг
/// </summary>
[Command("/ping")]
public sealed class PingHandler(IStateStorage store, ITransportClient tx, RequestIdProvider requestId) : IUpdateHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(UpdateContext ctx)
    {
        var key = $"ping:{ctx.User.Id}";
        var n = await store.IncrementAsync("user", key, 1, TimeSpan.FromDays(30), ctx.CancellationToken);
        await tx.SendTextAsync(ctx.Chat, $"pong #{n} (req: {requestId.Id})", ctx.CancellationToken);
    }
}