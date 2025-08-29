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
}