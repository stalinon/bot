namespace Stalinon.Bot.Core.Options;

/// <summary>
///     Опции дедупликации
/// </summary>
public sealed class DeduplicationOptions
{
    /// <summary>
    ///     Окно времени хранения идентификаторов
    /// </summary>
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Режим обработки дубликатов
    /// </summary>
    public RateLimitMode Mode { get; set; } = RateLimitMode.Hard;
}
