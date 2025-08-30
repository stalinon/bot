namespace Bot.Abstractions.Contracts;

/// <summary>
///     Источник обновлений
/// </summary>
public interface IUpdateSource
{
    /// <summary>
    ///     Запуск
    /// </summary>
    Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct);
}