using Microsoft.Extensions.Configuration;

namespace Bot.Hosting.Options;

/// <summary>
///     Настройки остановки фонового сервиса.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Определяет таймаут дренажа очереди при завершении.</item>
///     </list>
/// </remarks>
public sealed class StopOptions
{
    /// <summary>
    ///     Таймаут дренажа очереди в секундах.
    /// </summary>
    [ConfigurationKeyName("DRAIN_TIMEOUT_SECONDS")]
    public int DrainTimeoutSeconds { get; set; }
}

