using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Attributes;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Examples.HelloBot.Scenes;

namespace Stalinon.Bot.Examples.HelloBot.Handlers;

/// <summary>
///     Обработчик сцены профиля.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Запускает сцену по команде.</item>
///         <item>Перенаправляет сообщения в активную сцену.</item>
///     </list>
/// </remarks>
[Command("/profile")]
[TextMatch("^.+$")]
public sealed class ProfileHandler(ISceneNavigator navigator, ProfileScene scene, IFallbackHandler fallback)
    : IUpdateHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(UpdateContext ctx)
    {
        if (string.Equals(ctx.Command, "profile", StringComparison.OrdinalIgnoreCase))
        {
            await navigator.EnterAsync(ctx, scene);
            return;
        }

        var state = await navigator.GetStateAsync(ctx);
        if (state?.Scene == scene.Name)
        {
            await scene.OnUpdate(ctx);
            return;
        }

        await fallback.HandleAsync(ctx);
    }
}
