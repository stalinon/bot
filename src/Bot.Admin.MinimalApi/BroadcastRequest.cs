namespace Bot.Admin.MinimalApi;

/// <summary>
///     Запрос на рассылку сообщения.
/// </summary>
internal sealed class BroadcastRequest
{
    /// <summary>
    ///     Идентификаторы чатов, которым отправляется сообщение.
    /// </summary>
    public long[] ChatIds { get; init; } = Array.Empty<long>();
}

