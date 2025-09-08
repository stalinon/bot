using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Routing;
using Stalinon.Bot.Core.Stats;

namespace Stalinon.Bot.Core.Middlewares;

/// <summary>
///     Роутер для команд
/// </summary>
public sealed class RouterMiddleware(
    IServiceProvider sp,
    HandlerRegistry registry,
    StatsCollector stats,
    IFallbackHandler? fallbackHandler = null) : IUpdateMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var t = registry.FindFor(ctx);
        if (t is null)
        {
            if (fallbackHandler is not null)
            {
                await fallbackHandler.HandleAsync(ctx).ConfigureAwait(false);
            }
            else
            {
                await next(ctx).ConfigureAwait(false); // no handler matched
            }

            return;
        }

        var handler = (IUpdateHandler)sp.GetRequiredService(t);
        using var m = stats.Measure(t.Name);
        try
        {
            ctx.SetItem(UpdateItems.Handler, t.Name);
            await handler.HandleAsync(ctx).ConfigureAwait(false);
        }
        catch (Exception)
        {
            m.MarkError();
            throw;
        }
    }
}
