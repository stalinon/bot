using Bot.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Core.Pipeline;

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
                using var scope = sp.CreateScope();
                var mw = (IUpdateMiddleware)scope.ServiceProvider.GetRequiredService(typeof(T));
                await mw.InvokeAsync(ctx, next);
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
            _components.Add(component);
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

            return app;
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