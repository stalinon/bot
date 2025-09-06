using System;

namespace Bot.Core.Transport;

/// <summary>
///     Провайдер ключей сообщений на основе GUID.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Возвращает новый GUID в формате N.</item>
///     </list>
/// </remarks>
public sealed class GuidMessageKeyProvider : IMessageKeyProvider
{
    /// <inheritdoc />
    public string Next() => Guid.NewGuid().ToString("N");
}
