using Bot.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Core.Pipeline;

/// <summary>
///     Строитель пайплайна
/// </summary>
public sealed class PipelineBuilder(IServiceScopeFactory sp) : IUpdatePipeline
{
    private readonly IList<Func<UpdateDelegate, UpdateDelegate>> _components = new List<Func<UpdateDelegate, UpdateDelegate>>();

    /// <inheritdoc />
    public IUpdatePipeline Use<T>() where T : IUpdateMiddleware
    {
        _components.Add(next => async ctx =>
        {
            using var scope = sp.CreateScope();
            var mw = (IUpdateMiddleware)scope.ServiceProvider.GetRequiredService(typeof(T));
            await mw.InvokeAsync(ctx, next);
        });
        
        return this;
    }

    /// <inheritdoc />
    public IUpdatePipeline Use(Func<UpdateDelegate, UpdateDelegate> component)
    {
        _components.Add(component);
        return this;
    }

    /// <inheritdoc />
    public UpdateDelegate Build(UpdateDelegate terminal)
    {
        var app = terminal;
        for (var i = _components.Count - 1; i >= 0; i--)
        {
            app = _components[i](app);
        }

        return app;
    }
}