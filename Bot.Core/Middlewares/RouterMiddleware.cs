using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bot.Core.Middlewares;

/// <summary>
///     Роутер для команд
/// </summary>
public sealed class RouterMiddleware(IServiceProvider sp, HandlerRegistry registry, IFallbackHandler? fallbackHandler = null) : IUpdateMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var t = registry.FindFor(ctx);
        if (t is null)
        {
            if (fallbackHandler is not null)
            {
                await fallbackHandler.HandleAsync(ctx);
            }
            else
            {
                await next(ctx); // no handler matched
            }

            return;
        }

        var handler = (IUpdateHandler)sp.GetRequiredService(t);
        await handler.HandleAsync(ctx);
    }
}
