using System;

namespace Bot.Storage.EFCore;

/// <summary>
///     Запись состояния.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Хранит значение состояния</item>
///         <item>Содержит метаданные времени и версии</item>
///     </list>
/// </remarks>
public sealed class StateEntry
{
    /// <summary>
    ///     Область.
    /// </summary>
    public required string Scope { get; set; }

    /// <summary>
    ///     Ключ.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    ///     Значение.
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    ///     Время последнего обновления.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; }

    /// <summary>
    ///     Момент истечения срока действия.
    /// </summary>
    public DateTimeOffset? TtlUtc { get; set; }

    /// <summary>
    ///     Версия записи.
    /// </summary>
    public long Version { get; set; }
}
