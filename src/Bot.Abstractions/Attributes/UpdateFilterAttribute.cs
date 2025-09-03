namespace Bot.Abstractions.Attributes;

/// <summary>
///     Атрибут фильтра обновлений
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Позволяет выбирать обработчик по признакам</item>
///     </list>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UpdateFilterAttribute : Attribute
{
    /// <summary>
    ///     Признак данных веб-приложения
    /// </summary>
    public bool WebAppData { get; init; }
}
