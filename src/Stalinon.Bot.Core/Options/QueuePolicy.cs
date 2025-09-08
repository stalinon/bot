namespace Stalinon.Bot.Core.Options;

/// <summary>
///     Политика заполнения очереди.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Drop — новые элементы отбрасываются при переполнении.</item>
///         <item>Wait — производитель ждёт освобождения места.</item>
///     </list>
/// </remarks>
public enum QueuePolicy
{
    /// <summary>
    ///     Отбрасывать новые элементы при переполнении.
    /// </summary>
    Drop,

    /// <summary>
    ///     Ожидать освобождения места.
    /// </summary>
    Wait
}
