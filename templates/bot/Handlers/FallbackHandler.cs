using System.Threading.Tasks;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;

namespace BotApp.Handlers;

/// <summary>
///     Обработчик на неизвестный ввод.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Отвечает, когда сообщение не распознано.</item>
///     </list>
/// </remarks>
public sealed class FallbackHandler(ITransportClient client) : IFallbackHandler
{
    /// <inheritdoc />
    public Task HandleAsync(UpdateContext ctx)
    {
        return client.SendTextAsync(ctx.Chat, "не понимаю :(", ctx.CancellationToken);
    }
}
