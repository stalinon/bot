using System.Text.Json;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;

namespace Bot.Core.Scenes;

/// <summary>
///     Построитель сценариев пошагового мастера.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Позволяет описывать шаги с валидацией.</item>
///         <item>Поддерживает ветвление шагов.</item>
///         <item>Автоматизирует переходы и завершение сцены.</item>
///     </list>
/// </remarks>
public sealed class WizardSceneBuilder
{
    private readonly List<WizardStep> _steps = new();
    private Func<UpdateContext, IReadOnlyDictionary<int, string>, Task> _onFinish = (_, _) => Task.CompletedTask;

    /// <summary>
    ///     Добавить шаг мастера.
    /// </summary>
    /// <param name="prompt">Текст вопроса.</param>
    /// <param name="validator">Валидатор ответа.</param>
    /// <param name="ttl">Время жизни шага.</param>
    /// <param name="next">Функция выбора следующего шага.</param>
    /// <returns>Построитель для чейнинга.</returns>
    public WizardSceneBuilder AddStep(
        string prompt,
        Func<UpdateContext, string, Task<string?>> validator,
        TimeSpan? ttl = null,
        Func<IReadOnlyDictionary<int, string>, int?>? next = null)
    {
        _steps.Add(new WizardStep(prompt, validator, ttl, next));
        return this;
    }

    /// <summary>
    ///     Добавить шаг мастера с простым валидатором.
    /// </summary>
    /// <param name="prompt">Текст вопроса.</param>
    /// <param name="validator">Валидатор ответа.</param>
    /// <param name="error">Текст ошибки.</param>
    /// <param name="ttl">Время жизни шага.</param>
    /// <param name="next">Функция выбора следующего шага.</param>
    /// <returns>Построитель для чейнинга.</returns>
    public WizardSceneBuilder AddStep(
        string prompt,
        Func<string, bool> validator,
        string error = "Некорректное значение",
        TimeSpan? ttl = null,
        Func<IReadOnlyDictionary<int, string>, int?>? next = null)
    {
        return AddStep(
            prompt,
            (_, text) => Task.FromResult(validator(text) ? null : error),
            ttl,
            next);
    }

    /// <summary>
    ///     Задать обработчик завершения.
    /// </summary>
    /// <param name="handler">Действие при завершении.</param>
    /// <returns>Построитель для чейнинга.</returns>
    public WizardSceneBuilder OnFinish(
        Func<UpdateContext, IReadOnlyDictionary<int, string>, Task> handler)
    {
        _onFinish = handler;
        return this;
    }

    /// <summary>
    ///     Построить сцену.
    /// </summary>
    /// <param name="name">Имя сцены.</param>
    /// <param name="navigator">Навигатор сцен.</param>
    /// <param name="client">Клиент транспорта.</param>
    /// <returns>Построенная сцена.</returns>
    public IScene Build(string name, ISceneNavigator navigator, ITransportClient client)
    {
        return new WizardScene(name, navigator, client, _steps, _onFinish);
    }

    private sealed record WizardStep(
        string Prompt,
        Func<UpdateContext, string, Task<string?>> Validator,
        TimeSpan? Ttl,
        Func<IReadOnlyDictionary<int, string>, int?>? Next);

    private sealed class WizardScene : IScene
    {
        private readonly ITransportClient _client;
        private readonly ISceneNavigator _navigator;
        private readonly Func<UpdateContext, IReadOnlyDictionary<int, string>, Task> _onFinish;
        private readonly IReadOnlyList<WizardStep> _steps;

        public WizardScene(
            string name,
            ISceneNavigator navigator,
            ITransportClient client,
            IReadOnlyList<WizardStep> steps,
            Func<UpdateContext, IReadOnlyDictionary<int, string>, Task> onFinish)
        {
            Name = name;
            _navigator = navigator;
            _client = client;
            _steps = steps;
            _onFinish = onFinish;
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
            var first = _steps[0];
            await _client.SendTextAsync(ctx.Chat, first.Prompt, ctx.CancellationToken).ConfigureAwait(false);
            await _navigator.SetStepAsync(ctx, 1, ttl: first.Ttl).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task OnUpdate(UpdateContext ctx)
        {
            var state = await _navigator.GetStateAsync(ctx).ConfigureAwait(false);
            if (state is null || state.Scene != Name)
            {
                return;
            }

            var index = state.Step - 1;
            if (index < 0 || index >= _steps.Count)
            {
                return;
            }

            if (ctx.Text is null)
            {
                return;
            }

            var step = _steps[index];
            var error = await step.Validator(ctx, ctx.Text).ConfigureAwait(false);
            if (error is not null)
            {
                await _client.SendTextAsync(ctx.Chat, error, ctx.CancellationToken).ConfigureAwait(false);
                return;
            }

            var data = state.Data is null
                ? new Dictionary<int, string>()
                : JsonSerializer.Deserialize<Dictionary<int, string>>(state.Data) ?? new Dictionary<int, string>();
            data[index] = ctx.Text;
            var serialized = JsonSerializer.Serialize(data);
            await _navigator.SaveStepAsync(ctx, serialized, step.Ttl).ConfigureAwait(false);

            var nextIndex = step.Next is null ? index + 1 : step.Next(data);
            if (nextIndex is not null && nextIndex < _steps.Count)
            {
                var next = _steps[nextIndex.Value];
                await _client.SendTextAsync(ctx.Chat, next.Prompt, ctx.CancellationToken).ConfigureAwait(false);
                await _navigator.SetStepAsync(ctx, nextIndex.Value + 1, ttl: next.Ttl).ConfigureAwait(false);
            }
            else
            {
                await _onFinish(ctx, data).ConfigureAwait(false);
                await _navigator.ExitAsync(ctx).ConfigureAwait(false);
            }
        }
    }
}
