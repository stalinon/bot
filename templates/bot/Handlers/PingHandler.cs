using System.Threading.Tasks;
using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;

namespace BotApp.Handlers;

/// <summary>
///	Отвечает на /ping.
/// </summary>
/// <remarks>
///	<list type="number">
///		<item>Возвращает pong</item>
///	</list>
/// </remarks>
[Command("/ping")]
public sealed class PingHandler(ITransportClient tx) : IUpdateHandler
{
    /// <inheritdoc />
    public Task HandleAsync(UpdateContext ctx)
    {
        return tx.SendTextAsync(ctx.Chat, "pong", ctx.CancellationToken);
    }
}
