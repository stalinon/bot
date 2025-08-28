using Bot.Abstractions.Addresses;

namespace Bot.Abstractions.Contracts;

/// <summary>
///     Клиент рассылок
/// </summary>
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
}