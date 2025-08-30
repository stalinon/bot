using Bot.Abstractions;
using Bot.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Bot.Core.Middlewares;

/// <summary>
///     Логирование
/// </summary>
public sealed class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : IUpdateMiddleware
{
    /// <inheritdoc />
    public async Task InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["UpdateId"] = ctx.UpdateId
        });

        var updateType = ctx.GetItem<string>("UpdateType") ?? "unknown";
        var messageId = ctx.GetItem<int?>("MessageId");
        logger.LogInformation(
            "update {UpdateType} {UpdateId} message {MessageId} from {UserId} text='{Text}'",
            updateType,
            ctx.UpdateId,
            messageId,
            ctx.User.Id,
            ctx.Text);
        await next(ctx);
    }
}
