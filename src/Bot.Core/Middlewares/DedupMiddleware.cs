using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Utils;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Middlewares;

/// <summary>
///     Избавление от повторений
/// </summary>
public sealed class DedupMiddleware(ILogger<DedupMiddleware> logger, TtlCache<string> cache) : IUpdateMiddleware
{
    /// <inheritdoc />
    public Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var added = cache.TryAdd(ctx.UpdateId);
        if (!added)
        {
            logger.LogWarning("duplicate update {UpdateId} ignored", ctx.UpdateId);
            return Task.CompletedTask;
        }

        return next(ctx);
    }
}