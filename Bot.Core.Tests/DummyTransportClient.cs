using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;

namespace Bot.Core.Tests;

/// <summary>
///     Заглушка транспортного клиента для тестов.
/// </summary>
internal sealed class DummyTransportClient : ITransportClient
{
    /// <summary>
    ///     Заглушка отправки текста, ничего не делает.
    /// </summary>
    public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    ///     Заглушка отправки фото, ничего не делает.
    /// </summary>
    public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    ///     Заглушка редактирования текста, ничего не делает.
    /// </summary>
    public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    ///     Заглушка редактирования подписи, ничего не делает.
    /// </summary>
    public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    ///     Заглушка отправки действия чата, ничего не делает.
    /// </summary>
    public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    ///     Заглушка удаления сообщения, ничего не делает.
    /// </summary>
    public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct) => Task.CompletedTask;
}

