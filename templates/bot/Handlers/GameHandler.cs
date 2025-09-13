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
public sealed class GameHandler(ISceneNavigator navigator, GuessNumberScene scene) : IUpdateHandler
{
    /// <inheritdoc />
    public Task HandleAsync(UpdateContext ctx)
    {
        return navigator.EnterAsync(ctx, scene);
    }
}

