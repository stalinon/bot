using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Examples.HelloBot.Services;

namespace Stalinon.Bot.Examples.HelloBot.Handlers;

/// <summary>
///     Пинг.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Увеличивает счётчик пользователя</item>
///         <item>Отправляет ответ с порядковым номером</item>
///     </list>
/// </remarks>
[Command("/ping")]
public sealed class PingHandler(IStateStore store, ITransportClient tx, RequestIdProvider requestId) : IUpdateHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(UpdateContext ctx)
    {
        var key = $"ping:{ctx.User.Id}";
        var n = await store.IncrementAsync("user", key, 1, TimeSpan.FromDays(30), ctx.CancellationToken);
        await tx.SendTextAsync(ctx.Chat, $"pong #{n} (req: {requestId.Id})", ctx.CancellationToken);
    }
}
