using Bot.Abstractions;
using Bot.Abstractions.Contracts;

using Microsoft.Extensions.DependencyInjection;

namespace Bot.Core.Scenes;

/// <summary>
///     Расширения для <see cref="ISceneNavigator" />.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Содержит вспомогательные методы диалога.</item>
///         <item>Позволяет спрашивать пользователя и ждать ответ.</item>
///     </list>
/// </remarks>
public static class SceneNavigatorExtensions
{
    /// <summary>
    ///     Спросить текст и дождаться валидного ответа.
    /// </summary>
    /// <param name="navigator">Навигатор сцен.</param>
    /// <param name="ctx">Контекст обновления.</param>
    /// <param name="prompt">Текст вопроса.</param>
    /// <param name="validator">Валидатор ответа.</param>
    /// <param name="error">Текст ошибки.</param>
    /// <returns>Валидный ответ или <c>null</c>, если ещё ожидаем ввод.</returns>
    public static async Task<string?> AskTextAsync(
        this ISceneNavigator navigator,
        UpdateContext ctx,
        string prompt,
        Func<string, bool> validator,
        string error = "Некорректное значение")
    {
        var state = await navigator.GetStateAsync(ctx).ConfigureAwait(false)
                    ?? throw new InvalidOperationException("No active scene");
        var client = ctx.Services.GetRequiredService<ITransportClient>();

        if (state.Step == 0)
        {
            await client.SendTextAsync(ctx.Chat, prompt, ctx.CancellationToken).ConfigureAwait(false);
            await navigator.NextStepAsync(ctx).ConfigureAwait(false);
            return null;
        }

        if (state.Step != 1 || ctx.Text is null)
        {
            return null;
        }

        if (!validator(ctx.Text))
        {
            await client.SendTextAsync(ctx.Chat, error, ctx.CancellationToken).ConfigureAwait(false);
            return null;
        }

        await navigator.NextStepAsync(ctx, ctx.Text).ConfigureAwait(false);
        return ctx.Text;
    }
}
