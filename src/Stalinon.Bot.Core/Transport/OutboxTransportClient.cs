using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;
using Stalinon.Bot.Core.Utils;
using Stalinon.Bot.Outbox;

using Telegram.Bot;

namespace Stalinon.Bot.Core.Transport;

/// <summary>
///     Транспорт с аутбоксом.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Использует <see cref="IOutbox"/> для отложенной отправки.</item>
///         <item>Генерирует <c>MessageKey</c> и передаёт его в аутбокс.</item>
///         <item>Гарантирует повторную попытку и минимум одну доставку.</item>
///     </list>
/// </remarks>
public sealed class OutboxTransportClient(
    ITransportClient inner,
    IOutbox outbox,
    IMessageKeyProvider keys) : ITransportClient
{
    /// <summary>
    ///     Отправить текст.
    /// </summary>
    /// <inheritdoc />
    public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct)
    {
        var payload = JsonUtils.Serialize(new TextMessage(chat, text));
        return outbox.SendAsync(keys.Next(), payload, (_, data, token) =>
        {
            var msg = JsonUtils.Deserialize<TextMessage>(data)!;
            return inner.SendTextAsync(msg.Chat, msg.Text, token);
        }, ct);
    }

    /// <summary>
    ///     Отправить фото.
    /// </summary>
    /// <inheritdoc />
    public async Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await photo.CopyToAsync(ms, ct).ConfigureAwait(false);
        var payload = JsonUtils.Serialize(new PhotoMessage(chat, Convert.ToBase64String(ms.ToArray()), caption));
        await outbox.SendAsync(keys.Next(), payload, (_, data, token) =>
        {
            var msg = JsonUtils.Deserialize<PhotoMessage>(data)!;
            var bytes = Convert.FromBase64String(msg.Photo);
            return inner.SendPhotoAsync(msg.Chat, new MemoryStream(bytes), msg.Caption, token);
        }, ct).ConfigureAwait(false);
    }

    /// <summary>
    ///     Отредактировать текст.
    /// </summary>
    /// <inheritdoc />
    public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct)
    {
        var payload = JsonUtils.Serialize(new EditTextMessage(chat, messageId, text));
        return outbox.SendAsync(keys.Next(), payload, (_, data, token) =>
        {
            var msg = JsonUtils.Deserialize<EditTextMessage>(data)!;
            return inner.EditMessageTextAsync(msg.Chat, msg.MessageId, msg.Text, token);
        }, ct);
    }

    /// <summary>
    ///     Отредактировать подпись.
    /// </summary>
    /// <inheritdoc />
    public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct)
    {
        var payload = JsonUtils.Serialize(new EditCaptionMessage(chat, messageId, caption));
        return outbox.SendAsync(keys.Next(), payload, (_, data, token) =>
        {
            var msg = JsonUtils.Deserialize<EditCaptionMessage>(data)!;
            return inner.EditMessageCaptionAsync(msg.Chat, msg.MessageId, msg.Caption, token);
        }, ct);
    }

    /// <summary>
    ///     Показать действие.
    /// </summary>
    /// <inheritdoc />
    public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct)
    {
        var payload = JsonUtils.Serialize(new ActionMessage(chat, action));
        return outbox.SendAsync(keys.Next(), payload, (_, data, token) =>
        {
            var msg = JsonUtils.Deserialize<ActionMessage>(data)!;
            return inner.SendChatActionAsync(msg.Chat, msg.Action, token);
        }, ct);
    }

    /// <summary>
    ///     Удалить сообщение.
    /// </summary>
    /// <inheritdoc />
    public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct)
    {
        var payload = JsonUtils.Serialize(new DeleteMessage(chat, messageId));
        return outbox.SendAsync(keys.Next(), payload, (_, data, token) =>
        {
            var msg = JsonUtils.Deserialize<DeleteMessage>(data)!;
            return inner.DeleteMessageAsync(msg.Chat, msg.MessageId, token);
        }, ct);
    }

    /// <summary>
    ///     Отправить опрос.
    /// </summary>
    /// <inheritdoc />
    public Task SendPollAsync(ChatAddress chat, string question, IEnumerable<string> options, bool allowsMultipleAnswers, CancellationToken ct)
    {
        var payload = JsonUtils.Serialize(new PollMessage(chat, question, options.ToArray(), allowsMultipleAnswers));
        return outbox.SendAsync(keys.Next(), payload, (_, data, token) =>
        {
            var msg = JsonUtils.Deserialize<PollMessage>(data)!;
            return inner.SendPollAsync(msg.Chat, msg.Question, msg.Options, msg.AllowsMultipleAnswers, token);
        }, ct);
    }

    /// <summary>
    ///     Поставить реакцию.
    /// </summary>
    /// <inheritdoc />
    public Task SetMessageReactionAsync(ChatAddress chat, long messageId, IEnumerable<string> reactions, bool isBig, CancellationToken ct)
    {
        var payload = JsonUtils.Serialize(new ReactionMessage(chat, messageId, reactions.ToArray(), isBig));
        return outbox.SendAsync(keys.Next(), payload, (_, data, token) =>
        {
            var msg = JsonUtils.Deserialize<ReactionMessage>(data)!;
            return inner.SetMessageReactionAsync(msg.Chat, msg.MessageId, msg.Reactions, msg.IsBig, token);
        }, ct);
    }

    /// <summary>
    ///     Выполнить произвольное действие с нативным клиентом.
    /// </summary>
    /// <inheritdoc />
    public Task CallNativeClientAsync(Func<ITelegramBotClient, CancellationToken, Task> action, CancellationToken ct)
    {
        return inner.CallNativeClientAsync(action, ct);
    }

    private sealed record TextMessage(ChatAddress Chat, string Text);
    private sealed record PhotoMessage(ChatAddress Chat, string Photo, string? Caption);
    private sealed record EditTextMessage(ChatAddress Chat, long MessageId, string Text);
    private sealed record EditCaptionMessage(ChatAddress Chat, long MessageId, string? Caption);
    private sealed record ActionMessage(ChatAddress Chat, ChatAction Action);
    private sealed record DeleteMessage(ChatAddress Chat, long MessageId);
    private sealed record PollMessage(ChatAddress Chat, string Question, string[] Options, bool AllowsMultipleAnswers);
    private sealed record ReactionMessage(ChatAddress Chat, long MessageId, string[] Reactions, bool IsBig);
}
