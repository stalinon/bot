namespace Bot.Core.Options;

/// <summary>
///     Режим ответа при превышении лимитов
/// </summary>
public enum RateLimitMode
{
    /// <summary>
    ///     Мягкий режим: отвечает "помедленнее"
    /// </summary>
    Soft,

    /// <summary>
    ///     Жёсткий режим: молчит
    /// </summary>
    Hard
} 
