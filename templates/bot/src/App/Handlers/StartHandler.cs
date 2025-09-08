using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;

namespace BotApp.Handlers;

/// <summary>
///	Обрабатывает команду /start.
/// </summary>
/// <remarks>
///	<list type="number">
///		<item>Отправляет приветствие</item>
///	</list>
/// </remarks>
[Command("/start")]
public sealed class StartHandler(ITransportClient tx) : IUpdateHandler
{
    /// <inheritdoc />
    public Task HandleAsync(UpdateContext ctx)
    {
        return tx.SendTextAsync(ctx.Chat, "привет. напиши /ping", ctx.CancellationToken);
    }
}
