using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Examples.WebhookBot.Services;

namespace Stalinon.Bot.Examples.WebhookBot.Handlers;

/// <summary>
///     Пинг.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Считает количество запросов пользователя</item>
///         <item>Отправляет ответ с номером</item>
///     </list>
/// </remarks>
[Command("/ping")]
public sealed class PingHandler(IStateStore store, ITransportClient tx, RequestIdProvider requestId) : IUpdateHandler
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
