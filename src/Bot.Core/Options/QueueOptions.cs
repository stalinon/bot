namespace Bot.Core.Options;

/// <summary>
///     Настройки очереди обновлений.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Определяет политику обработки переполнения.</item>
///     </list>
/// </remarks>
public sealed class QueueOptions
{
    /// <summary>
    ///     Политика заполнения очереди.
    /// </summary>
    public QueuePolicy Policy { get; set; } = QueuePolicy.Wait;
}
