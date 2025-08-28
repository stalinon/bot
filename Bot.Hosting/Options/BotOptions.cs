using Bot.Core.Options;

namespace Bot.Hosting.Options;

/// <summary>
///     Настройки бота
/// </summary>
public sealed class BotOptions
{
    /// <summary>
    ///     Токен бота
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    ///     Тип получения данных
    /// </summary>
    public TransportMode Transport { get; set; } = TransportMode.Polling;
    
    /// <summary>
    ///     Параллелизм
    /// </summary>
    public int Parallelism { get; set; } = 8;
    
    /// <summary>
    ///     Ограничения
    /// </summary>
    public RateLimitOptions RateLimits { get; set; } = new();
}