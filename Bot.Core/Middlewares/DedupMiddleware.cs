using System.Collections.Concurrent;
using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Middlewares;

/// <summary>
///     Избавление от повторений
/// </summary>
public sealed class DedupMiddleware(ILogger<DedupMiddleware> logger) : IUpdateMiddleware
{
    private static readonly ConcurrentDictionary<string, DateTimeOffset> Seen = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    
    /// <inheritdoc />
    public Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var now = DateTimeOffset.UtcNow;
        var added = Seen.TryAdd(ctx.UpdateId, now);
        if (!added)
        {
            logger.LogWarning("duplicate update {UpdateId} ignored", ctx.UpdateId);
            return Task.CompletedTask;
        }
        
        _ = Task.Run(Cleanup);
        return next(ctx);
    }
    
    private static void Cleanup()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var kv in Seen)
        {
            if (now - kv.Value > Ttl)
            {
                Seen.TryRemove(kv.Key, out _);
            }
        }
    }
}