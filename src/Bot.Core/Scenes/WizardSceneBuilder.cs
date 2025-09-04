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
    /// <param name="error">Текст ошибки.</param>
    /// <param name="ttl">Время жизни шага.</param>
    /// <returns>Построитель для чейнинга.</returns>
    public WizardSceneBuilder AddStep(
        string prompt,
        Func<string, bool> validator,
        string error = "Некорректное значение",
        TimeSpan? ttl = null)
    {
        _steps.Add(new WizardStep(prompt, validator, error, ttl));
        return this;
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
        Func<string, bool> Validator,
        string Error,
        TimeSpan? Ttl);

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
            await _navigator.NextStepAsync(ctx, ttl: first.Ttl).ConfigureAwait(false);
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
            if (!step.Validator(ctx.Text))
            {
                await _client.SendTextAsync(ctx.Chat, step.Error, ctx.CancellationToken).ConfigureAwait(false);
                return;
            }

            var data = state.Data is null
                ? new Dictionary<int, string>()
                : JsonSerializer.Deserialize<Dictionary<int, string>>(state.Data) ?? new Dictionary<int, string>();
            data[index] = ctx.Text;
            var serialized = JsonSerializer.Serialize(data);
            await _navigator.SaveStepAsync(ctx, serialized, step.Ttl).ConfigureAwait(false);

            if (index + 1 < _steps.Count)
            {
                var next = _steps[index + 1];
                await _client.SendTextAsync(ctx.Chat, next.Prompt, ctx.CancellationToken).ConfigureAwait(false);
                await _navigator.NextStepAsync(ctx, ttl: next.Ttl).ConfigureAwait(false);
            }
            else
            {
                await _onFinish(ctx, data).ConfigureAwait(false);
                await _navigator.ExitAsync(ctx).ConfigureAwait(false);
            }
        }
    }
}
