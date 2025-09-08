namespace Stalinon.Bot.Core.Transport;

/// <summary>
///     Провайдер ключей сообщений.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Генерирует уникальный идентификатор для сообщения.</item>
///     </list>
/// </remarks>
public interface IMessageKeyProvider
{
    /// <summary>
    ///     Получить следующий ключ сообщения.
    /// </summary>
    string Next();
}
