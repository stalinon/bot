using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Core.Pipeline;

/// <summary>
///     Строитель пайплайна
/// </summary>
public sealed class PipelineBuilder(IServiceScopeFactory sp) : IUpdatePipeline
{
    private readonly List<Func<UpdateDelegate, UpdateDelegate>> _components = [];
    private readonly object _lock = new();
    private bool _built;

    /// <inheritdoc />
    public IUpdatePipeline Use<T>() where T : IUpdateMiddleware
    {
        lock (_lock)
        {
            EnsureNotBuilt();
            _components.Add(next => async ctx =>
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                var mw = ctx.Services.GetService<T>();
                if (mw == null)
                {
                    return;
                }

                await mw.InvokeAsync(ctx, next).ConfigureAwait(false);
            });

            return this;
        }
    }

    /// <inheritdoc />
    public IUpdatePipeline Use<T>(T middleware) where T : IUpdateMiddleware
    {
        lock (_lock)
        {
            EnsureNotBuilt();
            _components.Add(next => async ctx =>
            {
                ctx.CancellationToken.ThrowIfCancellationRequested();
                await middleware.InvokeAsync(ctx, next).ConfigureAwait(false);
            });

            return this;
        }
    }

    /// <inheritdoc />
    public IUpdatePipeline Use(Func<UpdateDelegate, UpdateDelegate> component)
    {
        lock (_lock)
        {
            EnsureNotBuilt();
            _components.Add(next =>
            {
                var del = component(next);
                return ctx =>
                {
                    ctx.CancellationToken.ThrowIfCancellationRequested();
                    return del(ctx);
                };
            });
            return this;
        }
    }

    /// <inheritdoc />
    public UpdateDelegate Build(UpdateDelegate terminal)
    {
        lock (_lock)
        {
            _built = true;
            var app = terminal;
            for (var i = _components.Count - 1; i >= 0; i--)
            {
                app = _components[i](app);
            }

            return async ctx =>
            {
                using var scope = sp.CreateScope();
                var scopedCtx = ctx with { Services = scope.ServiceProvider };
                await app(scopedCtx).ConfigureAwait(false);
            };
        }
    }

    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("Cannot add middleware after Build was called");
        }
    }
}
