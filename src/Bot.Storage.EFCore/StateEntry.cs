using System;

namespace Bot.Storage.EFCore;

/// <summary>
///     Запись состояния
/// </summary>
public sealed class StateEntry
{
    /// <summary>
    ///     Область
    /// </summary>
    public required string Scope { get; set; }

    /// <summary>
    ///     Ключ
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    ///     Значение
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    ///     Срок действия
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
