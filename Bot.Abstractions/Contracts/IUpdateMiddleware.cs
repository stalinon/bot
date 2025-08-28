namespace Bot.Abstractions.Contracts;

/// <summary>
///     Мидлварь обновления
/// </summary>
public interface IUpdateMiddleware
{
    /// <summary>
    ///     Активация
    /// </summary>
    Task InvokeAsync(UpdateContext ctx, UpdateDelegate next);
}