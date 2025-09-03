using Bot.Abstractions;
using Bot.Abstractions.Addresses;
using Bot.Abstractions.Contracts;

using Telegram.Bot;
using Telegram.Bot.Types;

namespace Bot.Telegram;

/// <summary>
///     Обертка над методами телеги
/// </summary>
public sealed class TelegramTransportClient(ITelegramBotClient client) : ITransportClient
{
    /// <inheritdoc />
    public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return client.SendMessage(chat.Id, text, cancellationToken: ct);
    }

    /// <inheritdoc />
    public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return client.SendPhoto(chat.Id, InputFile.FromStream(photo), caption, cancellationToken: ct);
    }

    /// <inheritdoc />
    public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return client.EditMessageText(chat.Id, (int)messageId, text, cancellationToken: ct);
    }

    /// <inheritdoc />
    public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return client.EditMessageCaption(chat.Id, (int)messageId, caption, cancellationToken: ct);
    }

    /// <inheritdoc />
    public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return client.SendChatAction(chat.Id, Map(action), cancellationToken: ct);
    }

    /// <inheritdoc />
    public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return client.DeleteMessage(chat.Id, (int)messageId, cancellationToken: ct);
    }

    private static global::Telegram.Bot.Types.Enums.ChatAction Map(ChatAction action) => action switch
    {
        ChatAction.Typing => global::Telegram.Bot.Types.Enums.ChatAction.Typing,
        ChatAction.UploadPhoto => global::Telegram.Bot.Types.Enums.ChatAction.UploadPhoto,
        ChatAction.RecordVideo => global::Telegram.Bot.Types.Enums.ChatAction.RecordVideo,
        ChatAction.UploadVideo => global::Telegram.Bot.Types.Enums.ChatAction.UploadVideo,
        ChatAction.RecordVoice => global::Telegram.Bot.Types.Enums.ChatAction.RecordVoice,
        ChatAction.UploadVoice => global::Telegram.Bot.Types.Enums.ChatAction.UploadVoice,
        ChatAction.UploadDocument => global::Telegram.Bot.Types.Enums.ChatAction.UploadDocument,
        ChatAction.FindLocation => global::Telegram.Bot.Types.Enums.ChatAction.FindLocation,
        ChatAction.RecordVideoNote => global::Telegram.Bot.Types.Enums.ChatAction.RecordVideoNote,
        ChatAction.UploadVideoNote => global::Telegram.Bot.Types.Enums.ChatAction.UploadVideoNote,
        ChatAction.ChooseSticker => global::Telegram.Bot.Types.Enums.ChatAction.ChooseSticker,
        _ => global::Telegram.Bot.Types.Enums.ChatAction.Typing,
    };
}
