using System;
using System.Threading.Tasks;

using BotApp.Scenes;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;

namespace BotApp.Handlers;

/// <summary>
///	Запускает игру /game.
/// </summary>
/// <remarks>
///	<list type="number">
///		<item>Переводит пользователя в сцену угадывания числа</item>
///	</list>
/// </remarks>
[Command("/game")]
[TextMatch("^[1-3]$")]
public sealed class GameHandler(ISceneNavigator navigator, GuessNumberScene scene, IFallbackHandler fallback)
    : IUpdateHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(UpdateContext ctx)
    {
        if (string.Equals(ctx.Command, "game", StringComparison.OrdinalIgnoreCase))
        {
            await navigator.EnterAsync(ctx, scene).ConfigureAwait(false);
            return;
        }

        var state = await navigator.GetStateAsync(ctx).ConfigureAwait(false);
        if (state?.Scene == scene.Name)
        {
            await scene.OnUpdate(ctx).ConfigureAwait(false);
            return;
        }

        await fallback.HandleAsync(ctx).ConfigureAwait(false);
    }
}

