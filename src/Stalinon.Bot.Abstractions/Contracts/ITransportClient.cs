using Stalinon.Bot.Abstractions.Addresses;

using Telegram.Bot;

namespace Stalinon.Bot.Abstractions.Contracts;

/// <summary>
///     Клиент рассылок
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Отправляет сообщения и действия пользователям.</item>
///         <item>Позволяет вызывать нативный <see cref="ITelegramBotClient"/>.</item>
///     </list>
/// </remarks>
public interface ITransportClient
{
    /// <summary>
    ///     Отправить текст
    /// </summary>
    Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct);

    /// <summary>
    ///     Отправить фото
    /// </summary>
    Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct);

    /// <summary>
    ///     Отредактировать сообщение
    /// </summary>
    Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct);

    /// <summary>
    ///     Отредактировать подпись сообщения
    /// </summary>
    Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct);

    /// <summary>
    ///     Показать действие бота
    /// </summary>
    Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct);

    /// <summary>
    ///     Удалить сообщение
    /// </summary>
    Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct);

    /// <summary>
    ///     Отправить опрос
    /// </summary>
    Task SendPollAsync(ChatAddress chat, string question, IEnumerable<string> options, bool allowsMultipleAnswers, CancellationToken ct);

    /// <summary>
    ///     Поставить реакцию на сообщение
    /// </summary>
    Task SetMessageReactionAsync(ChatAddress chat, long messageId, IEnumerable<string> reactions, bool isBig, CancellationToken ct);

    /// <summary>
    ///     Выполнить произвольное действие с нативным клиентом
    /// </summary>
    Task CallNativeClientAsync(Func<ITelegramBotClient, CancellationToken, Task> action, CancellationToken ct);
}
