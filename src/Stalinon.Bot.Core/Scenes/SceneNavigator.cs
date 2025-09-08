using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Core.Scenes;

/// <summary>
///     Навигатор по сценам.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Сохраняет состояние сцен в хранилище</item>
///         <item>Переходит между шагами с учётом TTL</item>
///     </list>
/// </remarks>
public sealed class SceneNavigator : ISceneNavigator
{
    private const string Scope = "scene";
    private readonly IStateStore _store;
    private readonly TimeSpan _ttl;

    /// <summary>
    ///     Создаёт навигатор.
    /// </summary>
    /// <param name="store">Хранилище состояний.</param>
    /// <param name="stepTtl">Время жизни шага.</param>
    public SceneNavigator(IStateStore store, TimeSpan? stepTtl = null)
    {
        _store = store;
        _ttl = stepTtl ?? TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    public async Task EnterAsync(UpdateContext ctx, IScene scene)
    {
        var now = DateTimeOffset.UtcNow;
        var state = new SceneState(ctx.User, ctx.Chat, scene.Name, 0, null, now, _ttl);
        await _store.SetAsync(Scope, Key(ctx), state, null, ctx.CancellationToken);
        await scene.OnEnter(ctx);
    }

    /// <inheritdoc />
    public async Task ExitAsync(UpdateContext ctx)
    {
        await _store.RemoveAsync(Scope, Key(ctx), ctx.CancellationToken);
    }

    /// <inheritdoc />
    public async Task<SceneState?> GetStateAsync(UpdateContext ctx)
    {
        var state = await _store.GetAsync<SceneState>(Scope, Key(ctx), ctx.CancellationToken)
            .ConfigureAwait(false);
        if (state is null)
        {
            return null;
        }

        if (state.Ttl is { } ttl && state.UpdatedAt.Add(ttl) <= DateTimeOffset.UtcNow)
        {
            await ExitAsync(ctx).ConfigureAwait(false);
            return null;
        }

        return state;
    }

    /// <inheritdoc />
    public async Task SaveStepAsync(UpdateContext ctx, string? data = null, TimeSpan? ttl = null)
    {
        while (true)
        {
            var state = await GetStateAsync(ctx).ConfigureAwait(false) ??
                        throw new InvalidOperationException("No active scene");
            var next = state with
            {
                Data = data,
                UpdatedAt = DateTimeOffset.UtcNow,
                Ttl = ttl ?? state.Ttl
            };
            var updated = await _store.TrySetIfAsync(Scope, Key(ctx), state, next, null, ctx.CancellationToken)
                .ConfigureAwait(false);
            if (updated)
            {
                return;
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> NextStepAsync(UpdateContext ctx, string? data = null, TimeSpan? ttl = null)
    {
        while (true)
        {
            var state = await GetStateAsync(ctx).ConfigureAwait(false) ??
                        throw new InvalidOperationException("No active scene");
            var next = state with
            {
                Step = state.Step + 1,
                Data = data ?? state.Data,
                UpdatedAt = DateTimeOffset.UtcNow,
                Ttl = ttl ?? _ttl
            };
            var updated = await _store.TrySetIfAsync(Scope, Key(ctx), state, next, null, ctx.CancellationToken)
                .ConfigureAwait(false);
            if (updated)
            {
                return next.Step;
            }
        }
    }

    /// <inheritdoc />
    public async Task SetStepAsync(UpdateContext ctx, int step, string? data = null, TimeSpan? ttl = null)
    {
        while (true)
        {
            var state = await GetStateAsync(ctx).ConfigureAwait(false) ??
                        throw new InvalidOperationException("No active scene");
            var next = state with
            {
                Step = step,
                Data = data ?? state.Data,
                UpdatedAt = DateTimeOffset.UtcNow,
                Ttl = ttl ?? _ttl
            };
            var updated = await _store.TrySetIfAsync(Scope, Key(ctx), state, next, null, ctx.CancellationToken)
                .ConfigureAwait(false);
            if (updated)
            {
                return;
            }
        }
    }

    private static string Key(UpdateContext ctx)
    {
        return $"{ctx.Transport}:{ctx.User.Id}:{ctx.Chat.Id}";
    }
}
