using Bot.Abstractions.Addresses;

namespace Bot.Abstractions;

/// <summary>
///     Контекст
/// </summary>
public sealed record UpdateContext(
    string Transport,
    string UpdateId,
    ChatAddress Chat,
    UserAddress User,
    string? Text,
    string? Command,
    string[]? Args,
    string? Payload,
    IDictionary<string, object> Items,
    IServiceProvider Services,
    CancellationToken CancellationToken)
{
    /// <summary>
    ///     Получить
    /// </summary>
    public T? GetItem<T>(string key) => Items.TryGetValue(key, out var v) ? (T?)v : default;
    
    /// <summary>
    ///     Установить
    /// </summary>
    public void SetItem(string key, object value) => Items[key] = value;
}