namespace Bot.Abstractions;

/// <summary>
///     Ключи элементов <see cref="UpdateContext"/>
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Используются для передачи метаданных между middleware</item>
///         <item>Исключают строковые литералы в коде</item>
///     </list>
/// </remarks>
public static class UpdateItems
{
    /// <summary>
    ///     Тип обновления
    /// </summary>
    public const string UpdateType = nameof(UpdateType);

    /// <summary>
    ///     Идентификатор сообщения
    /// </summary>
    public const string MessageId = nameof(MessageId);

    /// <summary>
    ///     Имя обработчика
    /// </summary>
    public const string Handler = nameof(Handler);

    /// <summary>
    ///     Признак данных веб-приложения
    /// </summary>
    public const string WebAppData = nameof(WebAppData);
}
