namespace Bot.Abstractions.Contracts;

/// <summary>
///     Обработчик обновления
/// </summary>
public interface IUpdateHandler
{
    /// <summary>
    ///     Обработать
    /// </summary>
    Task HandleAsync(UpdateContext ctx);
}
