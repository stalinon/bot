using System;

using System.Threading;
using System.Threading.Tasks;

namespace Stalinon.Bot.Outbox;

/// <summary>
///     Интерфейс отложенной отправки сообщений.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Записывает план перед фактической отправкой.</item>
///         <item>Подтверждает отправку после ответа транспорта.</item>
///         <item>Повторяет отправку с экспоненциальной паузой.</item>
///     </list>
/// </remarks>
public interface IOutbox
{
    /// <summary>
    ///     Отправить сообщение.
    /// </summary>
    /// <param name="id">Идентификатор сообщения.</param>
    /// <param name="payload">Полезная нагрузка.</param>
    /// <param name="transport">Функция отправки через транспорт.</param>
    /// <param name="ct">Токен отмены.</param>
    Task SendAsync(string id, string payload, Func<string, string, CancellationToken, Task> transport,
        CancellationToken ct);

    /// <summary>
    ///     Получить количество ожидающих сообщений.
    /// </summary>
    Task<long> GetPendingAsync(CancellationToken ct);
}
