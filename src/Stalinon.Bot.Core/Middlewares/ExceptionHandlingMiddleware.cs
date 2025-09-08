using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Contracts;

namespace Stalinon.Bot.Core.Middlewares;

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
