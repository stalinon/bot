namespace Bot.Core.Scenes;

using System;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Contracts;

/// <summary>
///     Навигатор по сценам.
/// </summary>
public sealed class SceneNavigator : ISceneNavigator
{
    private const string Scope = "scene";
    private readonly IStateStorage _store;

    /// <summary>
    ///     Создаёт навигатор.
    /// </summary>
    /// <param name="store">Хранилище состояний.</param>
    public SceneNavigator(IStateStorage store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task EnterAsync(UpdateContext ctx, IScene scene)
    {
        var state = new SceneState(ctx.User, ctx.Chat, scene.Name, 0);
        await _store.SetAsync(Scope, Key(ctx), state, null, ctx.CancellationToken);
        await _store.SetAsync(Scope, StepKey(ctx), 0L, null, ctx.CancellationToken);
        await scene.OnEnter(ctx);
    }

    /// <inheritdoc />
    public async Task ExitAsync(UpdateContext ctx)
    {
        await _store.RemoveAsync(Scope, Key(ctx), ctx.CancellationToken);
        await _store.RemoveAsync(Scope, StepKey(ctx), ctx.CancellationToken);
    }

    /// <inheritdoc />
    public Task<SceneState?> GetStateAsync(UpdateContext ctx)
    {
        return _store.GetAsync<SceneState>(Scope, Key(ctx), ctx.CancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> NextStepAsync(UpdateContext ctx)
    {
        var step = await _store.IncrementAsync(Scope, StepKey(ctx), 1, null, ctx.CancellationToken);
        var state = await GetStateAsync(ctx) ?? throw new InvalidOperationException("No active scene");
        var next = state with { Step = (int)step };
        await _store.SetAsync(Scope, Key(ctx), next, null, ctx.CancellationToken);
        return next.Step;
    }

    private static string Key(UpdateContext ctx) => $"{ctx.Transport}:{ctx.User.Id}:{ctx.Chat.Id}";
    private static string StepKey(UpdateContext ctx) => $"{Key(ctx)}:step";
}
