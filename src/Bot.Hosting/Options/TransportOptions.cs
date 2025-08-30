using System;

namespace Bot.Hosting.Options;

/// <summary>
///     Настройки транспорта
/// </summary>
public sealed class TransportOptions
{
    /// <summary>
    ///     Способ доставки обновлений
    /// </summary>
    public TransportMode Mode { get; set; } = TransportMode.Polling;

    /// <summary>
    ///     Публичный URL для вебхука
    /// </summary>
    public string? PublicUrl { get; set; }

    /// <summary>
    ///     Секрет для проверки вебхука
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    ///     Размер очереди входящих обновлений вебхука
    /// </summary>
    public int QueueCapacity { get; set; } = 1024;
}
