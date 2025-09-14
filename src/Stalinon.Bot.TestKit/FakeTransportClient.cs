using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;

using Telegram.Bot;

namespace Stalinon.Bot.TestKit;

/// <summary>
///     Фейковый клиент транспорта, сохраняющий отправленные сообщения в память.
/// </summary>
public sealed class FakeTransportClient : ITransportClient
{
    private readonly List<(ChatAddress chat, string text)> _texts = new();

    /// <summary>
    ///     Отправленные текстовые сообщения.
    /// </summary>
    public IReadOnlyList<(ChatAddress chat, string text)> SentTexts => _texts;

    /// <inheritdoc />
    public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct)
    {
        _texts.Add((chat, text));
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

    /// <inheritdoc />
    public Task SendPollAsync(ChatAddress chat, string question, IEnumerable<string> options, bool allowsMultipleAnswers, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetMessageReactionAsync(ChatAddress chat, long messageId, IEnumerable<string> reactions, bool isBig, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CallNativeClientAsync(Func<ITelegramBotClient, CancellationToken, Task> action, CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
