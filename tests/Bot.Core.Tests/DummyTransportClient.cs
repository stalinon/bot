using System.Diagnostics.CodeAnalysis;

using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;

namespace Bot.Core.Tests;

/// <summary>
///     Заглушка транспортного клиента для тестов.
/// </summary>
[SuppressMessage("Performance", "CA1822:Пометьте члены как статические")]
internal sealed class DummyTransportClient : ITransportClient
{
    /// <summary>
    ///     Сообщения для тестов
    /// </summary>
    public List<(ChatAddress chat, string text)> SentTexts { get; } = new();

    /// <inheritdoc />
    public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct)
    {
        SentTexts.Add((chat, text));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
