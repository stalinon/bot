using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Examples.WebhookBot.Handlers;

/// <summary>
///     Демонстрация индикатора печати.
/// </summary>
[Command("/typing")]
public sealed class TypingHandler(ITransportClient tx) : IUpdateHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(UpdateContext ctx)
    {
        await tx.SendChatActionAsync(ctx.Chat, ChatAction.Typing, ctx.CancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), ctx.CancellationToken);
        await tx.SendTextAsync(ctx.Chat, "готово", ctx.CancellationToken);
    }
}
