using System.Threading.Tasks;

using Bot.Abstractions;
using Bot.Abstractions.Contracts;

using Microsoft.Extensions.Logging;

namespace Bot.Core.Middlewares;

/// <summary>
///     Обработка ошибок
/// </summary>
public sealed class ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger) : IUpdateMiddleware
{
    /// <inheritdoc />
    public async ValueTask InvokeAsync(UpdateContext ctx, UpdateDelegate next)
    {
        try
        {
            await next(ctx).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ctx.CancellationToken.IsCancellationRequested)
        {
            /* swallow */
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "uncaught exception for update {UpdateId} user {UserId}", ctx.UpdateId, ctx.User.Id);
        }
    }
}
