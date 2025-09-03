namespace Bot.Hosting.Options;

/// <summary>
///     Настройки вебхука
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Публичный адрес</item>
///         <item>Секрет подписи</item>
///         <item>Очередь входящих обновлений</item>
///     </list>
/// </remarks>
public sealed class WebhookOptions
{
    /// <summary>
    ///     Публичный URL для вебхука
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    ///     Секрет для проверки вебхука
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    ///     Размер очереди входящих обновлений
    /// </summary>
    public int QueueCapacity { get; set; } = 1024;
}
