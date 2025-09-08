using System.Threading.Tasks;

namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Мидлварь обновления
/// </summary>
public interface IUpdateMiddleware
{
    /// <summary>
    ///     Активация
    /// </summary>
    ValueTask InvokeAsync(UpdateContext ctx, UpdateDelegate next);
}
