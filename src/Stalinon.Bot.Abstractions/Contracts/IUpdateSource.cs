namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Источник обновлений
/// </summary>
public interface IUpdateSource
{
    /// <summary>
    ///     Запуск
    /// </summary>
    Task StartAsync(Func<UpdateContext, Task> onUpdate, CancellationToken ct);

    /// <summary>
    ///     Остановка
    /// </summary>
    Task StopAsync();
}
