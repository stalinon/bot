namespace Bot.Hosting.Options;

/// <summary>
///     Тип получения данных
/// </summary>
public enum TransportMode
{
    /// <summary>
    ///     Полинг
    /// </summary>
    Polling,
    
    /// <summary>
    ///     Хук
    /// </summary>
    Webhook
}