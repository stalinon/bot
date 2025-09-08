namespace Stalinon.Bot.Scheduler;

/// <summary>
///     Фоновая задача.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Выполняется планировщиком</item>
///         <item>Не должна блокировать поток</item>
///     </list>
/// </remarks>
public interface IJob
{
    /// <summary>
    ///     Выполнить задачу.
    /// </summary>
    Task ExecuteAsync(CancellationToken ct);
}
