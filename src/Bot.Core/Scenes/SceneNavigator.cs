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
    private readonly IStateStore _store;

    /// <summary>
    ///     Создаёт навигатор.
    /// </summary>
    /// <param name="store">Хранилище состояний.</param>
    public SceneNavigator(IStateStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task EnterAsync(UpdateContext ctx, IScene scene)
    {
        var state = new SceneState(ctx.User, ctx.Chat, scene.Name, 0);
        await _store.SetAsync(Scope, Key(ctx), state, null, ctx.CancellationToken);
        await scene.OnEnter(ctx);
    }

    /// <inheritdoc />
    public Task ExitAsync(UpdateContext ctx)
    {
        return _store.RemoveAsync(Scope, Key(ctx), ctx.CancellationToken);
    }

    /// <inheritdoc />
    public Task<SceneState?> GetStateAsync(UpdateContext ctx)
    {
        return _store.GetAsync<SceneState>(Scope, Key(ctx), ctx.CancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> NextStepAsync(UpdateContext ctx)
    {
        while (true)
        {
            var state = await GetStateAsync(ctx);
            if (state is null)
            {
                throw new InvalidOperationException("No active scene");
            }

            var next = state with { Step = state.Step + 1 };
            if (await _store.TrySetIfAsync(Scope, Key(ctx), state, next, ctx.CancellationToken))
            {
                return next.Step;
            }
        }
    }

    private static string Key(UpdateContext ctx) => $"{ctx.Transport}:{ctx.User.Id}:{ctx.Chat.Id}";
}
