using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Bot.Core.Middlewares;

/// <summary>
///     Логирование
/// </summary>
public sealed class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : IUpdateMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        logger.LogInformation("update {UpdateId} from {UserId} text='{Text}'", ctx.UpdateId, ctx.User.Id, ctx.Text);
        await next(ctx);
    }
}