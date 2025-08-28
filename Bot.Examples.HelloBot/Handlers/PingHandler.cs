using Bot.Abstractions;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;

namespace Bot.Examples.HelloBot.Handlers;

/// <summary>
///     Пинг
/// </summary>
[Command("/ping")]
public sealed class PingHandler(IStateStore store, ITransportClient tx) : IUpdateHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(UpdateContext ctx)
    {
        var key = $"ping:{ctx.User.Id}";
        var n = await store.GetAsync<int>("user", key, ctx.CancellationToken);
        n++;
        await store.SetAsync("user", key, n, TimeSpan.FromDays(30), ctx.CancellationToken);
        await tx.SendTextAsync(ctx.Chat, $"pong #{n}", ctx.CancellationToken);
    }
}