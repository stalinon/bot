using System.Threading.Tasks;

namespace Bot.Abstractions.Contracts;

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
