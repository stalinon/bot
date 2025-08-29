using Bot.Core.Options;
using System;

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
    ///     Настройки транспорта
    /// </summary>
    public TransportOptions Transport { get; set; } = new();
    
    /// <summary>
    ///     Параллелизм
    /// </summary>
    public int Parallelism { get; set; } = 8;
    
    /// <summary>
    ///     Ограничения
    /// </summary>
    public RateLimitOptions RateLimits { get; set; } = new();

    /// <summary>
    ///     Время жизни записей для дедупликации
    /// </summary>
    public TimeSpan DeduplicationTtl { get; set; } = TimeSpan.FromMinutes(5);
}