using Bot.Abstractions;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;
using Bot.Examples.WebhookBot.Services;

namespace Bot.Examples.WebhookBot.Handlers;

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
        var n = await store.GetAsync<int>("user", key, ctx.CancellationToken);
        n++;
        await store.SetAsync("user", key, n, TimeSpan.FromDays(30), ctx.CancellationToken);
        await tx.SendTextAsync(ctx.Chat, $"pong #{n} (req: {requestId.Id})", ctx.CancellationToken);
    }
}