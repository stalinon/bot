using Bot.Abstractions;
using Bot.Abstractions.Attributes;
using Bot.Abstractions.Contracts;
using Bot.Examples.HelloBot.Scenes;

namespace Bot.Examples.HelloBot.Handlers;

/// <summary>
///     Обработчик команды и шагов сцены ввода телефона.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Запускает сцену по команде.</item>
///         <item>Перенаправляет сообщения в активную сцену.</item>
///     </list>
/// </remarks>
[Command("/phone")]
[TextMatch("^.+$")]
public sealed class PhoneHandler(ISceneNavigator navigator, PhoneScene scene, IFallbackHandler fallback)
    : IUpdateHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(UpdateContext ctx)
    {
        if (string.Equals(ctx.Command, "phone", StringComparison.OrdinalIgnoreCase))
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
