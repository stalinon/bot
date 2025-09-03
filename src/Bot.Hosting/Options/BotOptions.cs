using Bot.Core.Options;
using System;

namespace Bot.Hosting.Options;

/// <summary>
///     Настройки бота
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Параметры доступа</item>
///         <item>Настройки транспорта</item>
///         <item>Ограничения и дедупликация</item>
///     </list>
/// </remarks>
public sealed class BotOptions
{
    /// <summary>
    ///     Токен бота
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    ///     Токен доступа к административным эндпоинтам
    /// </summary>
    public string AdminToken { get; set; } = string.Empty;

    /// <summary>
    ///     Настройки транспорта
    /// </summary>
    public TransportOptions Transport { get; set; } = new();

    /// <summary>
    ///     Ограничения
    /// </summary>
    public RateLimitOptions RateLimits { get; set; } = new();

    /// <summary>
    ///     Время жизни записей для дедупликации
    /// </summary>
    public TimeSpan DeduplicationTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Таймаут дренажа очереди при остановке
    /// </summary>
    public TimeSpan DrainTimeout { get; set; } = TimeSpan.FromSeconds(5);
}