namespace Stalinon.Bot.Core.Options;

/// <summary>
///     Опции ограничения ддоса
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>
    ///     Кол-во запросов пользователя в минуту
    /// </summary>
    public int PerUserPerMinute { get; set; } = 20;

    /// <summary>
    ///     Кол-во запросов из чата в минуту
    /// </summary>
    public int PerChatPerMinute { get; set; } = 60;

    /// <summary>
    ///     Режим ограничения
    /// </summary>
    public RateLimitMode Mode { get; set; } = RateLimitMode.Hard;

    /// <summary>
    ///     Использовать распределённое хранилище состояний.
    /// </summary>
    public bool UseStateStore { get; set; }
        = false;

    /// <summary>
    ///     Окно времени для подсчёта запросов.
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
