using System.Collections.Concurrent;
using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Bot.Core.Options;

namespace Bot.Core.Middlewares;

/// <summary>
///     Рейт-лиметер
/// </summary>
public sealed class RateLimitMiddleware(RateLimitOptions options) : IUpdateMiddleware
{
    private readonly ConcurrentDictionary<long, Queue<DateTimeOffset>> _user = new();
    private readonly ConcurrentDictionary<long, Queue<DateTimeOffset>> _chat = new();

    /// <inheritdoc />
    public Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        var now = DateTimeOffset.UtcNow;
        if (!Check(_user, ctx.User.Id, options.PerUserPerMinute, now))
        {
            return Task.CompletedTask;
        }

        if (!Check(_chat, ctx.Chat.Id, options.PerChatPerMinute, now))
        {
            return Task.CompletedTask;
        }

        return next(ctx);
    }
    
    private static bool Check(ConcurrentDictionary<long, Queue<DateTimeOffset>> dict, long key, int limit, DateTimeOffset now)
    {
        var q = dict.GetOrAdd(key, _ => new Queue<DateTimeOffset>(limit + 1));
        lock (q)
        {
            while (q.Count > 0 && now - q.Peek() > TimeSpan.FromMinutes(1))
            {
                q.Dequeue();
            }

            if (q.Count >= limit)
            {
                return false;
            }

            q.Enqueue(now);
            return true;
        }
    }
}