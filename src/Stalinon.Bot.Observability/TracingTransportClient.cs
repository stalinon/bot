using Stalinon.Bot.Abstractions;
using Stalinon.Bot.Abstractions.Addresses;
using Stalinon.Bot.Abstractions.Contracts;

using Telegram.Bot;

namespace Stalinon.Bot.Observability;

/// <summary>
///     Декоратор транспорта с трассировкой.
/// </summary>
/// <remarks>
///     <list type="number">
///         <item>Создаёт спаны отправки сообщений.</item>
///     </list>
/// </remarks>
public sealed class TracingTransportClient(ITransportClient inner) : ITransportClient
{
    /// <inheritdoc />
    public Task SendTextAsync(ChatAddress chat, string text, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(SendTextAsync));
        activity?.SetTag("chat.id", chat.Id);
        return inner.SendTextAsync(chat, text, ct);
    }

    /// <inheritdoc />
    public Task SendPhotoAsync(ChatAddress chat, Stream photo, string? caption, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(SendPhotoAsync));
        activity?.SetTag("chat.id", chat.Id);
        return inner.SendPhotoAsync(chat, photo, caption, ct);
    }

    /// <inheritdoc />
    public Task EditMessageTextAsync(ChatAddress chat, long messageId, string text, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(EditMessageTextAsync));
        activity?.SetTag("chat.id", chat.Id);
        return inner.EditMessageTextAsync(chat, messageId, text, ct);
    }

    /// <inheritdoc />
    public Task EditMessageCaptionAsync(ChatAddress chat, long messageId, string? caption, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(EditMessageCaptionAsync));
        activity?.SetTag("chat.id", chat.Id);
        return inner.EditMessageCaptionAsync(chat, messageId, caption, ct);
    }

    /// <inheritdoc />
    public Task SendChatActionAsync(ChatAddress chat, ChatAction action, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(SendChatActionAsync));
        activity?.SetTag("chat.id", chat.Id);
        activity?.SetTag("action", action.ToString());
        return inner.SendChatActionAsync(chat, action, ct);
    }

    /// <inheritdoc />
    public Task DeleteMessageAsync(ChatAddress chat, long messageId, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(DeleteMessageAsync));
        activity?.SetTag("chat.id", chat.Id);
        return inner.DeleteMessageAsync(chat, messageId, ct);
    }

    /// <inheritdoc />
    public Task SendPollAsync(ChatAddress chat, string question, IEnumerable<string> options, bool allowsMultipleAnswers, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(SendPollAsync));
        activity?.SetTag("chat.id", chat.Id);
        return inner.SendPollAsync(chat, question, options, allowsMultipleAnswers, ct);
    }

    /// <inheritdoc />
    public Task SetMessageReactionAsync(ChatAddress chat, long messageId, IEnumerable<string> reactions, bool isBig, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(SetMessageReactionAsync));
        activity?.SetTag("chat.id", chat.Id);
        return inner.SetMessageReactionAsync(chat, messageId, reactions, isBig, ct);
    }

    /// <inheritdoc />
    public Task CallNativeClientAsync(Func<ITelegramBotClient, CancellationToken, Task> action, CancellationToken ct)
    {
        using var activity = Telemetry.ActivitySource.StartActivity("Transport/Send");
        activity?.SetTag("method", nameof(CallNativeClientAsync));
        return inner.CallNativeClientAsync(action, ct);
    }
}
