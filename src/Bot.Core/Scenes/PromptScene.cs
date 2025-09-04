using Bot.Abstractions;
using Bot.Abstractions.Contracts;

using Microsoft.Extensions.DependencyInjection;

namespace Bot.Core.Scenes;

/// <summary>
///     Простая сцена: задаёт вопрос, ждёт ответ и проверяет его валидатором.
/// </summary>
public sealed class PromptScene : IScene
{
    private readonly string _error;
    private readonly ISceneNavigator _navigator;
    private readonly Func<UpdateContext, string, Task> _onSuccess;
    private readonly string _prompt;
    private readonly Func<string, bool> _validator;

    /// <summary>
    ///     Создаёт сцену.
    /// </summary>
    /// <param name="navigator">Навигатор сцен.</param>
    /// <param name="name">Название сцены.</param>
    /// <param name="prompt">Текст вопроса.</param>
    /// <param name="validator">Валидатор ответа.</param>
    /// <param name="onSuccess">Обработчик успешного ответа.</param>
    /// <param name="error">Текст ошибки.</param>
    public PromptScene(
        ISceneNavigator navigator,
        string name,
        string prompt,
        Func<string, bool> validator,
        Func<UpdateContext, string, Task> onSuccess,
        string error = "Некорректное значение")
    {
        _navigator = navigator;
        Name = name;
        _prompt = prompt;
        _validator = validator;
        _onSuccess = onSuccess;
        _error = error;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public Task<bool> CanEnter(UpdateContext ctx)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public async Task OnEnter(UpdateContext ctx)
    {
        var client = ctx.Services.GetRequiredService<ITransportClient>();
        await client.SendTextAsync(ctx.Chat, _prompt, ctx.CancellationToken).ConfigureAwait(false);
        await _navigator.NextStepAsync(ctx).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task OnUpdate(UpdateContext ctx)
    {
        var state = await _navigator.GetStateAsync(ctx).ConfigureAwait(false);
        if (state?.Step != 1)
        {
            return;
        }

        if (ctx.Text is null)
        {
            return;
        }

        if (_validator(ctx.Text))
        {
            await _onSuccess(ctx, ctx.Text).ConfigureAwait(false);
            await _navigator.ExitAsync(ctx).ConfigureAwait(false);
        }
        else
        {
            var client = ctx.Services.GetRequiredService<ITransportClient>();
            await client.SendTextAsync(ctx.Chat, _error, ctx.CancellationToken).ConfigureAwait(false);
        }
    }
}
