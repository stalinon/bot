namespace Bot.Hosting.Options;

/// <summary>
///     Настройки транспорта
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Выбор способа доставки</item>
///         <item>Параллелизм обработки</item>
///         <item>Параметры вебхука</item>
///     </list>
/// </remarks>
public sealed class TransportOptions
{
    /// <summary>
    ///     Способ доставки обновлений
    /// </summary>
    public TransportMode Mode { get; set; } = TransportMode.Polling;

    /// <summary>
    ///     Максимальное число параллельных обработчиков
    /// </summary>
    public int Parallelism { get; set; } = 8;

    /// <summary>
    ///     Настройки вебхука
    /// </summary>
    public WebhookOptions Webhook { get; set; } = new();
}
